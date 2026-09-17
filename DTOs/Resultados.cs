namespace JcaInventario.DTOs;

public record FotoDisponivel(string Caminho, string TipoConteudo);
public record DocumentoDisponivel(string Caminho, string TipoConteudo, string NomeOriginal);
public record ArquivoGerado(byte[] Conteudo, string TipoConteudo, string Nome);
public record UsuarioAutenticado(int Id, string Nome, bool Administrador);
