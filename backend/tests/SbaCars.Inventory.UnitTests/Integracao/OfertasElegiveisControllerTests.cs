using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using SbaCars.BuildingBlocks.Web.Auth;
using SbaCars.Inventory.Api.Controllers;

namespace SbaCars.Inventory.UnitTests.Integracao;

public sealed class OfertasElegiveisControllerTests
{
    [Fact]
    public void Listar_DeclaresServiceIntegrationPermissionPolicy()
    {
        var method = typeof(OfertasElegiveisController).GetMethod(
            nameof(OfertasElegiveisController.Listar));

        method.Should().NotBeNull();
        var authorization = method!
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        authorization.Policy.Should().Be(Permissoes.EstoqueIntegrar);
    }
}
