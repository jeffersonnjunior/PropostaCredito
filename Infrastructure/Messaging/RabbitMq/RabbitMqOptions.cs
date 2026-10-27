namespace Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "credito";
    public string Password { get; set; } = "credito123";

    /// <summary>Exchange de onde consome ClienteCadastrado (mesmo do Cadastro).</summary>
    public string ConsumeExchange { get; set; } = "credito.clientes";
    public string ConsumeRoutingKey { get; set; } = "cliente.cadastrado";
    public string ConsumeQueue { get; set; } = "proposta.cliente-cadastrado";
    public string ConsumeDlq { get; set; } = "proposta.cliente-cadastrado.dlq";

    /// <summary>Exchange para publicar PropostaGerada.</summary>
    public string PublishExchange { get; set; } = "credito.propostas";
    public string PublishRoutingKey { get; set; } = "proposta.gerada";

    public bool Enabled { get; set; } = true;
}
