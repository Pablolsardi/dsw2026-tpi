# Trabajo Práctico Integrador — Desarrollo de Software 2026

### Integrantes
- Litninsky, José Ignacio - 56710
- López Roldán, Fabrizzio Javier - 56450
- López Sardi, Pablo - 56037
- Luna Mendoza, Diego Elías - 56142

## Requisitos
- .NET 10 SDK
- SQL Server LocalDB

## Configuración y ejecución
1. Clonar el repositorio y ubicarse en la raíz.
2. La cadena de conexión está en `Dsw2026Tpi.Api/appsettings.Development.json`
   (`ConnectionStrings:DefaultConnection`). Ajustar si no se usa LocalDB.
3. Aplicar las migraciones de ambos contextos:
   dotnet ef database update -p Dsw2026Tpi.Data -s Dsw2026Tpi.Api -c AuthenticationDbContext
   dotnet ef database update -p Dsw2026Tpi.Data -s Dsw2026Tpi.Api -c Dsw2026TpiDbContext
4. Ejecutar: dotnet run --project Dsw2026Tpi.Api
5. Swagger disponible en /swagger.

El usuario administrador se crea automáticamente al iniciar, con las credenciales
definidas en `AdminUser` (por defecto admin@system.com / Admin1234!).

## Autenticación
Todos los endpoints requieren JWT salvo los dos de login.
Enviar el token como header: `Authorization: Bearer <token>`.
Roles: ADMINISTRADOR y PACIENTE.

## Endpoints
| Método | Ruta | Rol | Descripción |
|---|---|---|---|
| POST | /api/auth/admin/login | — | Login de administrador (email + password) |
| POST | /api/auth/patient/login | — | Login de paciente (email + dni). Lo registra si no existe |
| GET | /api/specialties?pageSize=&pageIndex=&name= | Autenticado | Lista paginada de especialidades activas |
| POST | /api/specialties | Admin | Crea una especialidad |
| PUT | /api/specialties/{id} | Admin | Actualiza una especialidad |
| DELETE | /api/specialties/{id} | Admin | Baja lógica |
| GET | /api/doctors?pageSize=&pageIndex=&name= | Autenticado | Lista paginada de médicos activos |
| GET | /api/doctors/{id}/availabilities | Autenticado | Rangos horarios del médico en el mes actual |
| POST | /api/doctors | Admin | Crea un médico |
| PUT | /api/doctors/{id} | Admin | Actualiza un médico |
| DELETE | /api/doctors/{id} | Admin | Baja lógica |
| POST | /api/availabilities | Admin | Genera turnos de 30 min para el resto del mes |
| PUT | /api/availabilities | Admin | Sobrescribe las disponibilidades futuras no reservadas |
| POST | /api/appointments | Paciente | Reserva un turno |
| GET | /api/appointments/patient?dni= | Paciente | Turnos activos del paciente |
| DELETE | /api/appointments/{id} | Paciente | Cancela un turno |
| GET | /api/appointments?date=YYYY-MM-DD | Admin | Turnos de un día |
| GET | /api/appointments/search?pageSize=&pageIndex=&specialtyId=&doctorId=&dni=&date= | Admin | Búsqueda combinada |

Los errores siguen el formato `{ errorCode, message, details[] }`.