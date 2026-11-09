# TechSupport Solutions — Mini API de incidentes

ASP.NET Core Web API (.NET 10) · EF Core + SQLite · JWT · BCrypt.
Centraliza los incidentes de soporte, controla quién puede hacer qué y responde con errores seguros.

## Cómo ejecutar

Requisito: **.NET 10 SDK** (`dotnet --version` → 10.x).

```bash
cd TechSupport.API
dotnet run --launch-profile http
```

- La API queda en **http://localhost:5131**.
- **Swagger UI:** http://localhost:5131/swagger — haz login en `POST /api/auth/login`, copia el `token`,
  pulsa **Authorize** y pégalo.
- También puedes usar [`TechSupport.API/TechSupport.API.http`](TechSupport.API/TechSupport.API.http) (VS Code REST Client / Visual Studio / Rider) o Postman.
- La base de datos `techsupport.db` (SQLite) se crea sola al arrancar (`EnsureCreated`) junto con los usuarios
  de prueba. Para empezar de cero, borra el archivo y vuelve a ejecutar.

Pruebas automáticas con evidencia (con la API corriendo y BD nueva, desde la raíz del repo):

```bash
./scripts/pruebas.sh        # genera docs/evidencias/*.txt y RESUMEN.md
node scripts/capturas.js    # (opcional) genera las capturas PNG
```

## Usuarios de prueba

| Id | Usuario | Contraseña | Rol |
|---|---|---|---|
| 1 | `admin` | `Admin123!` | Administrador |
| 2 | `tecnico1` | `Tecnico123!` | Tecnico |
| 3 | `tecnico2` | `Tecnico123!` | Tecnico |
| 4 | `usuario1` | `Usuario123!` | Usuario |
| 5 | `usuario2` | `Usuario123!` | Usuario |

Las contraseñas se guardan como **hash BCrypt**, nunca en texto plano.

## Endpoints

| Método | Endpoint | Roles | Qué hace |
|---|---|---|---|
| POST | `/api/auth/login` | Anónimo | Devuelve un JWT (60 min) |
| POST | `/api/incidentes` | Todos | Crea incidente (estado `Pendiente`) |
| GET | `/api/incidentes` | Todos* | Lista según el rol |
| GET | `/api/incidentes/{id}` | Todos* | Detalle, solo si le corresponde (si no, 404) |
| PUT | `/api/incidentes/{id}/asignar` | Administrador | Asigna técnico `{ "tecnicoId": 2 }` |
| PUT | `/api/incidentes/{id}/estado` | Admin, Técnico* | Avanza el estado `{ "nuevoEstado": "EnProceso" }` |
| DELETE | `/api/incidentes/{id}` | Administrador | Elimina (no permitido si está `Cerrado`) |

\* Admin ve todo; Técnico solo los asignados a él; Usuario solo los que creó.
Valores: `prioridad` = `Baja|Media|Alta|Critica`; `estado` = `Pendiente → Asignado → EnProceso → Resuelto → Cerrado`.

## ¿Dónde va la clave del JWT en un proyecto real?

En este proyecto de clase la clave (`Jwt:Key`) está en `appsettings.Development.json` **solo por comodidad**.
En un proyecto real **nunca se sube a GitHub**: cualquiera con acceso al repositorio podría firmar tokens válidos
(por ejemplo, de Administrador).

- **Desarrollo:** `dotnet user-secrets` (se guarda fuera del repositorio, en el perfil del usuario):
  ```bash
  cd TechSupport.API
  dotnet user-secrets init
  dotnet user-secrets set "Jwt:Key" "una-clave-larga-y-aleatoria-de-al-menos-32-bytes"
  ```
- **Producción:** variable de entorno `Jwt__Key` (o un gestor de secretos como Azure Key Vault / AWS Secrets Manager).

La API no arranca si la clave falta o tiene menos de 32 bytes (requisito de HMAC-SHA256).

## Estructura

```
TechSupport.API/
├── Controllers/   AuthController (login) · IncidentesController (solo traduce HTTP ↔ servicio)
├── Services/      IncidenteService (TODAS las reglas de propiedad e integridad) · TokenService (JWT)
├── Middleware/    ExceptionHandlingMiddleware (500 seguro con requestId)
├── Data/          AppDbContext (dos relaciones Incidente↔Usuario) · DbSeeder (usuarios iniciales)
├── Models/        Usuario · Incidente · enums Rol, Prioridad, EstadoIncidente
├── DTOs/          entrada/salida (nunca se devuelve la entidad Usuario → no se filtra PasswordHash)
└── Extensions/    lectura del id y rol desde los claims del token
docs/
├── ANALISIS.md    Hoja de análisis: tabla A1, diagrama de estados A2, decisiones A3
├── PRUEBAS.md     Tabla de pruebas con evidencia y capturas
└── evidencias/    Petición/respuesta de cada prueba (.txt) y capturas (.png)
scripts/           pruebas.sh (ejecuta las pruebas) · capturas.js (genera PNG)
```

## Decisiones técnicas (por qué · qué pasaría si no)

| Decisión | Por qué | Qué pasaría si no |
|---|---|---|
| Incidente ajeno → **404** (no 403) | No confirma que el recurso existe. | Un atacante podría enumerar ids y saber qué incidentes existen. |
| El 403 solo cuando el **rol** no permite la operación | Se rechaza antes de mirar el incidente; no revela nada. | — |
| No se elimina un incidente **Cerrado** | Es el historial: auditoría y métricas de resolución. | Se pierde la trazabilidad y la evidencia si el problema vuelve. |
| Id de quien actúa leído **del token** | El token está firmado por el servidor; no se puede falsificar. | Cualquiera podría enviar otro `usuarioId` en el cuerpo y actuar en nombre de otro. |
| Mismo **401** si el usuario no existe o la contraseña es incorrecta (y mismo tiempo de respuesta) | No revela qué usuarios existen. | Un atacante sabría qué nombres de usuario son válidos y atacaría solo esos. |
| Reglas en un único **`IncidenteService`** | Si una regla cambia, se cambia una sola vez. | Reglas copiadas en cada endpoint acaban siendo inconsistentes. |
| `[Authorize(Roles=...)]` **y** comprobación de rol en el servicio | Defensa en profundidad: la API rechaza la petición aunque venga directa. | Un error en un atributo dejaría la operación abierta. |
| **DTOs** de salida | Se controla exactamente qué datos salen. | Devolver `Usuario` filtraría el `PasswordHash`. |
| Contraseñas con **BCrypt** | Hash lento y con sal: resistente a fuerza bruta y tablas rainbow. | Con una fuga de la BD se expondrían todas las contraseñas. |
| `ExceptionHandlingMiddleware` **al inicio** del pipeline | Envuelve a todo lo que viene después. | Al final no capturaría excepciones de los middlewares registrados antes (autenticación, autorización, Swagger…), que llegarían al cliente sin formato seguro ni `requestId`. |
| `requestId` = `HttpContext.TraceIdentifier` | Soporte localiza el error en el log sin mostrar nada sensible. | Sin él no habría forma de relacionar la queja de un usuario con el error del log. |
| Enums guardados como **texto** en la BD | La BD es legible (`"EnProceso"` en vez de `2`). | Reordenar el enum cambiaría el significado de los datos guardados. |
| `UseAuthentication()` antes de `UseAuthorization()` | Primero "¿quién eres?", luego "¿qué puedes hacer?". | La autorización no conocería al usuario y rechazaría todo (o nada). |

## Entregables

- [x] Repositorio Git con al menos 4 commits (2 por sesión) — ver `git log`.
- [x] Proyecto `TechSupport.API` completo.
- [x] Hoja de análisis: [docs/ANALISIS.md](docs/ANALISIS.md) (tabla A1, diagrama A2, decisiones A3).
- [x] Tabla de pruebas con capturas: [docs/PRUEBAS.md](docs/PRUEBAS.md).
- [x] README (este archivo).

## Criterios de aceptación (con su evidencia)

- [x] La API usa SQLite y Entity Framework Core → `Data/AppDbContext.cs`, `appsettings.json`.
- [x] El login devuelve un JWT válido y las contraseñas están con hash → pruebas 1, 2, 10; `DbSeeder.cs` (BCrypt).
- [x] Los endpoints protegidos rechazan peticiones sin token → pruebas 3 y 26.
- [x] Los roles restringen asignar y eliminar → pruebas 4 y 12.
- [x] Técnicos y usuarios solo ven y modifican lo que les corresponde → pruebas 7, 14, 15, 22, 23.
- [x] No se cierra un incidente sin resolverlo ni se asigna a quien no es técnico → pruebas 8, 11, 16, 20.
- [x] Los errores no revelan información interna y llevan un `requestId` → prueba 9 (+ log del servidor).
- [x] El proyecto se ejecuta siguiendo este README.
