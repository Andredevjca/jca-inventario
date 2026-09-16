using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace JcaInventario.Configuracoes;

public sealed class MensagensValidacaoPortugues : IBindingMetadataProvider, IValidationMetadataProvider
{
    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        var nome = context.Attributes.OfType<DisplayAttribute>().FirstOrDefault()?.GetName()
            ?? context.Key.Name ?? "informado";
        var mensagens = context.BindingMetadata.ModelBindingMessageProvider ??= new DefaultModelBindingMessageProvider();
        mensagens.SetValueMustNotBeNullAccessor(_ => $"O campo {nome} é obrigatório.");
        mensagens.SetAttemptedValueIsInvalidAccessor((_, campo) => $"Informe um valor válido para o campo {campo}.");
        mensagens.SetUnknownValueIsInvalidAccessor(campo => $"Informe um valor válido para o campo {campo}.");
        mensagens.SetValueIsInvalidAccessor(_ => $"Informe um valor válido para o campo {nome}.");
        mensagens.SetValueMustBeANumberAccessor(campo => $"O campo {campo} deve ser um número.");
        mensagens.SetMissingBindRequiredValueAccessor(campo => $"O campo {campo} é obrigatório.");
        mensagens.SetMissingKeyOrValueAccessor(() => "Informe a chave e o valor obrigatórios.");
        mensagens.SetMissingRequestBodyRequiredValueAccessor(() => "Informe os dados obrigatórios da solicitação.");
        mensagens.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "Informe um valor válido.");
        mensagens.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Informe um valor válido.");
        mensagens.SetNonPropertyValueMustBeANumberAccessor(() => "Informe um número válido.");
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var atributo in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            // Preserva mensagens específicas e traduções definidas por recursos.
            var mensagemPadraoEmail = atributo is EmailAddressAttribute
                && atributo.ErrorMessage == new EmailAddressAttribute().ErrorMessage;
            if ((atributo.ErrorMessage != null && !mensagemPadraoEmail) || atributo.ErrorMessageResourceName != null
                || atributo.ErrorMessageResourceType != null)
                continue;

            var mensagem = atributo switch
            {
                RequiredAttribute => "O campo {0} é obrigatório.",
                StringLengthAttribute { MinimumLength: > 0 } => "O campo {0} deve ter entre {2} e {1} caracteres.",
                StringLengthAttribute => "O campo {0} deve ter no máximo {1} caracteres.",
                RangeAttribute { Minimum: int minimo, Maximum: int maximo } when minimo == 1 && maximo == int.MaxValue
                    => "Selecione uma opção válida para o campo {0}.",
                RangeAttribute => "O campo {0} deve estar entre {1} e {2}.",
                EmailAddressAttribute => "Informe um e-mail válido para o campo {0}.",
                _ => null
            };
            if (mensagem != null)
                atributo.ErrorMessage = mensagem;
        }
    }
}
