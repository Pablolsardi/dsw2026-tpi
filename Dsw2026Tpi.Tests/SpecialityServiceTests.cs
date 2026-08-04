using System.Linq.Expressions;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Services;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using NSubstitute;

namespace Dsw2026Tpi.Tests;

public class SpecialityServiceTests
{
    private readonly IPersistence _mockPersistence = Substitute.For<IPersistence>();
    private readonly SpecialityService _service;

    private const string NombreValido = "Cardiologia";
    private const string DescripcionValida = "Estudio y tratamiento del corazon";

    public SpecialityServiceTests()
    {
        _service = new SpecialityService(_mockPersistence);
    }

    [Fact]
    public async Task Create_CuandoLaEspecialidadNoExiste_EntoncesLaPersisteYRetornaElResponse()
    {
        //Arrange
        _mockPersistence
            .FirstIgnoringFilters(Arg.Any<Expression<Func<Speciality, bool>>>())
            .Returns((Speciality?)null);

        var request = new SpecialityModel.Request(NombreValido, DescripcionValida);

        //Act
        var resultado = await _service.Create(request);

        //Assert
        Assert.Equal(NombreValido, resultado.Name);
        Assert.Equal(DescripcionValida, resultado.Description);
        Assert.NotEqual(Guid.Empty, resultado.Id);
        await _mockPersistence.Received(1).Add(Arg.Any<Speciality>());
    }

    [Fact]
    public async Task Create_CuandoExisteUnaEspecialidadEliminada_EntoncesLaRestauraEnLugarDeCrearla()
    {
        //Arrange
        var eliminada = new Speciality(NombreValido, "Descripcion anterior")
        {
            Deleted = true
        };

        _mockPersistence
            .FirstIgnoringFilters(Arg.Any<Expression<Func<Speciality, bool>>>())
            .Returns(eliminada);

        var request = new SpecialityModel.Request(NombreValido, DescripcionValida);

        //Act
        var resultado = await _service.Create(request);

        //Assert
        Assert.False(eliminada.Deleted);
        Assert.Equal(DescripcionValida, resultado.Description);
        Assert.Equal(eliminada.Id, resultado.Id);
        await _mockPersistence.Received(1).Update(eliminada);
        await _mockPersistence.DidNotReceive().Add(Arg.Any<Speciality>());
    }

    [Fact]
    public async Task Create_CuandoYaExisteUnaEspecialidadActivaConEseNombre_EntoncesLanzaConflictException()
    {
        //Arrange
        var activa = new Speciality(NombreValido, DescripcionValida);

        _mockPersistence
            .FirstIgnoringFilters(Arg.Any<Expression<Func<Speciality, bool>>>())
            .Returns(activa);

        var request = new SpecialityModel.Request(NombreValido, DescripcionValida);

        //Act y Assert
        await Assert.ThrowsAsync<ConflictException>(() => _service.Create(request));
        await _mockPersistence.DidNotReceive().Add(Arg.Any<Speciality>());
    }

    [Theory]
    [InlineData("AB", "Descripcion suficientemente larga")]
    [InlineData("Cardiologia", "Corta")]
    [InlineData("Cardiologia", "Esta descripcion supera holgadamente los cien caracteres permitidos por la validacion del servicio de especialidades")]
    public async Task Create_CuandoLosDatosNoCumplenLasValidaciones_EntoncesLanzaValidationException(
        string nombre, string descripcion)
    {
        //Arrange
        var request = new SpecialityModel.Request(nombre, descripcion);

        //Act y Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.Create(request));
        await _mockPersistence.DidNotReceive().Add(Arg.Any<Speciality>());
    }
}