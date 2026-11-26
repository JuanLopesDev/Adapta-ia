using System.Net;
using System.Net.Mail;

namespace Adapta.Web.Services;

public sealed class EmailService(IConfiguration config, IWebHostEnvironment ambiente, ILogger<EmailService> log)
{
    public bool CaixaLocal => ambiente.IsDevelopment() && config.GetValue<bool>("Email:CaixaLocal")
        && string.IsNullOrWhiteSpace(config["Smtp:Host"]);

    public static string PastaLocal => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Adapta", "Emails");

    public async Task<bool> EnviarAsync(string destino, string assunto, string texto)
    {
        var smtp = config.GetSection("Smtp");
        if (!CaixaLocal && (string.IsNullOrWhiteSpace(smtp["Host"]) || string.IsNullOrWhiteSpace(smtp["Remetente"])))
        {
            log.LogWarning("Configure Smtp:Host e Smtp:Remetente para habilitar a entrega de e-mail.");
            return false;
        }
        try
        {
            using var cliente = new SmtpClient();
            if (CaixaLocal)
            {
                Directory.CreateDirectory(PastaLocal);
                cliente.DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                cliente.PickupDirectoryLocation = PastaLocal;
            }
            else
            {
                cliente.Host = smtp["Host"]!;
                cliente.Port = smtp.GetValue("Porta", 587);
                cliente.EnableSsl = !ambiente.IsDevelopment() || smtp.GetValue("StartTls", true);
                if (!string.IsNullOrEmpty(smtp["Usuario"]))
                    cliente.Credentials = new NetworkCredential(smtp["Usuario"], smtp["Senha"]);
            }
            using var mensagem = new MailMessage(smtp["Remetente"] ?? "adapta@localhost", destino, assunto, texto);
            mensagem.BodyEncoding = System.Text.Encoding.UTF8;
            mensagem.BodyTransferEncoding = System.Net.Mime.TransferEncoding.QuotedPrintable;
            using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await cliente.SendMailAsync(mensagem, limite.Token);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or OperationCanceledException or InvalidOperationException or ArgumentException or IOException or UnauthorizedAccessException)
        {
            log.LogWarning("A entrega SMTP falhou ({Tipo}).", ex.GetType().Name);
            return false;
        }
    }
}
