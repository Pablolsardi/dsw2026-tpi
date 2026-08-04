using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Tests;

public class SpecialityTests
{
    private const string NombreValido = "Cardiologia";
    private const string DescripcionValida = "Estudio y tratamiento del corazon";

    [Fact]
    public void Constructor_CuandoRecibeNombreYDescripcion_EntoncesAsignaLosValores()
    {
        //Arrange & Act
        var speciality = new Speciality(NombreValido, DescripcionValida);

        //Assert
        Assert.Equal(NombreValido, speciality.Name);
        Assert.Equal(DescripcionValida, speciality.Description);
    }

    [Fact]
    public void Constructor_CuandoNoRecibeId_EntoncesGeneraUnoNuevo()
    {
        var speciality = new Speciality(NombreValido, DescripcionValida);

        Assert.NotEqual(Guid.Empty, speciality.Id);
    }

    [Fact]
    public void Constructor_CuandoRecibeUnId_EntoncesLoConserva()
    {
        var id = Guid.NewGuid();

        var speciality = new Speciality(NombreValido, DescripcionValida, id);

        Assert.Equal(id, speciality.Id);
    }

    [Fact]
    public void Constructor_CuandoSeCreaLaEspecialidad_EntoncesNoEstaEliminada()
    {
        var speciality = new Speciality(NombreValido, DescripcionValida);

        Assert.False(speciality.Deleted);
    }

    [Theory]
    [InlineData("Neurologia", "Diagnostico de afecciones cerebrales")]
    [InlineData("Pediatria", "Cuidado medico de lactantes y ninos")]
    public void Update_CuandoRecibeNuevosValores_EntoncesActualizaNombreYDescripcion(
        string nuevoNombre, string nuevaDescripcion)
    {
        var speciality = new Speciality(NombreValido, DescripcionValida);

        speciality.Update(nuevoNombre, nuevaDescripcion);

        Assert.Equal(nuevoNombre, speciality.Name);
        Assert.Equal(nuevaDescripcion, speciality.Description);
    }

    [Fact]
    public void Update_CuandoSeActualizaLaEspecialidad_EntoncesConservaElId()
    {
        var id = Guid.NewGuid();
        var speciality = new Speciality(NombreValido, DescripcionValida, id);

        speciality.Update("Dermatologia", "Enfermedades de la piel");

        Assert.Equal(id, speciality.Id);
    }

    [Fact]
    public void Restore_CuandoLaEspecialidadEstaEliminada_EntoncesQuedaActiva()
    {
        var speciality = new Speciality(NombreValido, DescripcionValida)
        {
            Deleted = true
        };

        speciality.Restore();

        Assert.False(speciality.Deleted);
    }
}