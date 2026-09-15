namespace JcaInventario.Helpers;

public static class OpcoesInventario
{
    public static readonly string[] Status =
    [
        "Disponível",
        "Em uso",
        "Home Office",
        "Em manutenção",
        "Em estoque",
        "Inativo",
        "Baixado"
    ];

    public static readonly string[] Localizacoes =
    [
        "Escritório",
        "Home Office",
        "Estoque",
        "Manutenção",
        "Outro"
    ];

    public static readonly string[] Movimentacoes =
    [
        "Entrega",
        "Devolução",
        "Transferência",
        "Transferência para estoque",
        "Alteração de localização"
    ];

    public static readonly string[] TiposTrabalho =
    [
        "Presencial",
        "Home Office",
        "Híbrido"
    ];
}

public enum TipoCadastro
{
    Setor,
    TipoEquipamento
}
