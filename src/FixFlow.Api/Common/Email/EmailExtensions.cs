using FixFlow.Api.Common.OpenApi;

namespace FixFlow.Api.Common.Email;

public static class EmailExtensions
{
    public static IServiceCollection AddEmailSending(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .Validate(options => options.IsValid(), "Email configuration is invalid. Host, Port and FromAddress are required when email sending is enabled.")
            .ValidateOnStartOutsideBuildTimeGeneration();

        if (configuration.GetValue<bool>($"{EmailOptions.SectionName}:{nameof(EmailOptions.Enabled)}"))
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, DisabledEmailSender>();
        }

        return services;
    }
}
