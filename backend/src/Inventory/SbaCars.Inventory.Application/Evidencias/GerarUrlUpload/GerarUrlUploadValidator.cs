using FluentValidation;

namespace SbaCars.Inventory.Application.Evidencias.GerarUrlUpload;

public sealed class GerarUrlUploadValidator : AbstractValidator<GerarUrlUploadCommand>
{
    public GerarUrlUploadValidator()
    {
        RuleFor(command => command.OfertaId)
            .NotEmpty()
            .WithMessage("ofertaId é obrigatório.");

        RuleFor(command => command.NomeArquivo)
            .NotEmpty()
            .WithMessage("nomeArquivo é obrigatório.")
            .MaximumLength(255)
            .WithMessage("nomeArquivo não pode exceder 255 caracteres.");

        RuleFor(command => command.TipoConteudo)
            .NotEmpty()
            .WithMessage("tipoConteudo é obrigatório.");

        RuleFor(command => command.TamanhoBytes)
            .GreaterThan(0)
            .WithMessage("tamanhoBytes deve ser maior que zero.");
    }
}
