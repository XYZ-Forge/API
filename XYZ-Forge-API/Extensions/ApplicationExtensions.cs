namespace XYZForge.Extensions
{
    public static class ApplicationExtensions
    {
        public static void ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "XYZ-Forge-API");
                    c.RoutePrefix = "api";
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.Use(async (context, next) => {
                if(context.Request.Path == "/") {
                    context.Response.Redirect("/api");
                } else {
                    await next();
                }
            });
        }
    }
}
