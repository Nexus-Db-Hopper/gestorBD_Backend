# Gestor de BD - API Backend

API RESTful construida con **.NET 9** y ASP.NET Core, diseñada siguiendo los principios de **Arquitectura Limpia (Clean Architecture)**. Proporciona una base sólida y segura para la gestión de usuarios y autenticación utilizando JSON Web Tokens (JWT) con soporte para Refresh Tokens y un sistema de logout seguro.

## Características Principales

-   **Autenticación Segura con JWT**: Implementación completa de Access Tokens y Refresh Tokens.
-   **Logout del Lado del Servidor**: Invalidación de Refresh Tokens para un cierre de sesión seguro.
-   **Autorización Basada en Roles**: Endpoints protegidos que requieren roles específicos (ej. "Admin").
-   **Arquitectura Limpia**: Separación estricta de responsabilidades entre las capas de Dominio, Aplicación, Infraestructura y API.
-   **Hashing de Contraseñas**: Uso de `BCrypt.Net` para almacenar las contraseñas de forma segura.
-   **Entity Framework Core 9**: Interacción con la base de datos MySQL a través del proveedor Pomelo.
-   **Documentación de API con Swagger**: Interfaz de usuario interactiva para probar los endpoints de la API, configurada para manejar la autenticación JWT.

---

## Arquitectura del Proyecto

El proyecto sigue una estricta estructura de Arquitectura Limpia, donde las dependencias fluyen hacia el centro (Dominio).

```
Api/ → Application/ → Domain ←
Infrastructure/ → Application/ → Domain ←
```

-   **`Domain`**: Contiene las entidades puras del negocio (`User`, `Role`) y las interfaces de los repositorios (si se usaran). Es el núcleo y no depende de ninguna otra capa.
-   **`Application`**: Contiene la lógica de la aplicación, definiendo los "casos de uso" a través de interfaces (`IAuthService`, `IJwtService`) y los DTOs que sirven como contratos de datos.
-   **`Infrastructure`**: Implementa las interfaces de la capa de Aplicación. Contiene la lógica de acceso a la base de datos (`AppDbContext`), la implementación de servicios externos (`JwtService`, `AuthService`) y otras preocupaciones técnicas.
-   **`Api`**: Es el punto de entrada de la aplicación. Contiene los Controladores, la configuración del middleware y el arranque de la aplicación (`Program.cs`).

---

## Cómo Empezar

Sigue estos pasos para levantar el entorno de desarrollo local.

### Prerrequisitos

-   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
-   Una instancia de base de datos MySQL en ejecución.

### 1. Configuración

-   Clona este repositorio.
-   Abre el archivo `nexusDB.Api/appsettings.Development.json`.
-   Modifica la cadena de conexión `DefaultConnection` para que apunte a tu base de datos MySQL.

    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=localhost;Database=nexusdb_dev;User=root;Password=tu_contraseña;"
    }
    ```

### 2. Configuración de la Base de Datos

Usa Entity Framework Core para crear la base de datos y las tablas a partir de las entidades del proyecto.

Abre una terminal en la raíz del proyecto (`C:/Users/Msuthy/Desktop/gestorBD_Backend/`) y ejecuta los siguientes comandos:

1.  **Crear la migración inicial:**
    ```sh
    dotnet ef migrations add InitialCreate --project nexusDB.Infrastructure
    ```

2.  **Aplicar la migración a la base de datos:**
    ```sh
    dotnet ef database update --project nexusDB.Infrastructure
    ```

### 3. Ejecutar la Aplicación

```sh
dotnet run --project nexusDB.Api
```

La API estará disponible en `https://localhost:XXXX` (la URL se mostrará en la consola). La documentación de Swagger estará disponible en `https://localhost:XXXX/swagger`.

---

## Endpoints de la API de Autenticación

Todos los endpoints se encuentran bajo la ruta base `/api/auth`.

### `POST /register`

Registra un nuevo usuario en el sistema.

-   **Body:** `RegisterUserDto`
    ```json
    {
      "email": "test@example.com",
      "password": "password123",
      "name": "Test",
      "lastName": "User"
    }
    ```
-   **Respuesta Exitosa:** `201 Created`

### `POST /login`

Autentica a un usuario y devuelve un Access Token y un Refresh Token.

-   **Body:** `LoginUserDto`
    ```json
    {
      "email": "test@example.com",
      "password": "password123"
    }
    ```
-   **Respuesta Exitosa:** `200 OK` con un `TokenResponseDto`
    ```json
    {
      "accessToken": "ey...",
      "refreshToken": "ey..."
    }
    ```

### `POST /refresh`

Genera un nuevo par de tokens (Access y Refresh) a partir de un Refresh Token válido.

-   **Body:** `string` (el refresh token en texto plano)
-   **Respuesta Exitosa:** `200 OK` con un nuevo `TokenResponseDto`.

### `POST /logout`

Cierra la sesión del usuario invalidando su Refresh Token en la base de datos.

-   **Autorización:** Requiere un `AccessToken` válido.
-   **Respuesta Exitosa:** `204 No Content`.

### `GET /profile`

Endpoint protegido que devuelve la información del usuario autenticado a partir de los claims del token.

-   **Autorización:** Requiere un `AccessToken` válido.
-   **Respuesta Exitosa:** `200 OK` con los datos del perfil.

### `GET /admin-data`

Endpoint protegido que solo es accesible para usuarios con el rol "Admin".

-   **Autorización:** Requiere un `AccessToken` de un usuario con el rol "Admin".
-   **Respuesta Exitosa:** `200 OK`.
-   **Respuesta de Acceso Denegado:** `403 Forbidden` si el usuario no tiene el rol requerido.

---

## Flujo de Prueba en Swagger

1.  **Registra un usuario** usando el endpoint `POST /register`.
2.  **Inicia sesión** con ese usuario usando `POST /login`. Copia el `accessToken` y el `refreshToken` de la respuesta.
3.  **Autoriza Swagger**: Haz clic en el botón `Authorize` en la parte superior derecha. En el diálogo, escribe `Bearer TU_ACCESS_TOKEN` (reemplazando `TU_ACCESS_TOKEN` con el token que copiaste) y haz clic en `Authorize`.
4.  **Accede al perfil**: Ejecuta `GET /profile`. Deberías recibir una respuesta `200 OK`.
5.  **Prueba el rol**: Ejecuta `GET /admin-data`. Deberías recibir una respuesta `403 Forbidden`, ya que el usuario por defecto tiene el rol "User".
6.  **Refresca la sesión**: Usa el `refreshToken` en el endpoint `POST /refresh`. Recibirás un nuevo par de tokens.
7.  **Cierra sesión**: Asegurándote de que sigues autorizado con un token válido, ejecuta `POST /logout`. Recibirás una respuesta `204 No Content`.
8.  **Verifica el logout**: Intenta usar el `refreshToken` original de nuevo en `POST /refresh`. Deberías recibir una respuesta `401 Unauthorized`, confirmando que el token fue invalidado.


# Librerias Docker usadas (temporal, borrar al terminar)
El proyecto "nexusDB.Api" tiene las referencias de paquete siguientes
[net9.0]:
Paquete de nivel superior                            Solicitado           Resuelto
> Microsoft.AspNetCore.Authentication.JwtBearer      9.0.0-rc.2.24470.3   9.0.0-rc.2.24474.3
> Microsoft.AspNetCore.OpenApi                       9.0.11               9.0.11            
> Swashbuckle.AspNetCore.Filters                     9.0.0-beta.2         9.0.0             
> Swashbuckle.AspNetCore.SwaggerUI                   9.0.6                9.0.6

El proyecto "nexusDB.Application" tiene las referencias de paquete siguientes
[net9.0]: No se encontró ningún paquete para este marco.
El proyecto "nexusDB.Domain" tiene las referencias de paquete siguientes
[net9.0]: No se encontró ningún paquete para este marco.
El proyecto "nexusDB.Infrastructure" tiene las referencias de paquete siguientes                                                                                  
[net9.0]:
Paquete de nivel superior                   Solicitado   Resuelto
> BCrypt.Net-Next                           4.0.3        4.0.3   
> Docker.DotNet                             3.125.15     3.125.15
> Microsoft.EntityFrameworkCore             9.0.0        9.0.0   
> Microsoft.EntityFrameworkCore.Design      9.0.0        9.0.0   
> Pomelo.EntityFrameworkCore.MySql          9.0.0        9.0.0   
> System.IdentityModel.Tokens.Jwt           8.14.0       8.14.0  