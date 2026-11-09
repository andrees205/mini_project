namespace TechSupport.API.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var requestId = context.TraceIdentifier;

            logger.LogError(ex,
                "Excepción no controlada. RequestId: {RequestId}. {Method} {Path}",
                requestId, context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                success = false,
                message = "Ocurrió un error al procesar la solicitud.",
                requestId,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });
        }
    }
}
