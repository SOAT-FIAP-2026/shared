using Fiap.TechChallenge.Contracts.Common;
using FluentAssertions;

namespace Fiap.TechChallenge.Contracts.Tests;

public class PaginationContractsTests
{
    [Fact]
    public void PagedRequestContrato_deve_usar_valores_padrao()
    {
        var req = new PagedRequestContrato();
        req.Page.Should().Be(1);
        req.PageSize.Should().Be(10);
        req.Skip.Should().Be(0);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public void PagedRequestContrato_deve_ajustar_pagina_invalida(int inputPage, int expectedPage)
    {
        var req = new PagedRequestContrato(inputPage, 10);
        req.Page.Should().Be(expectedPage);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-10, 10)]
    [InlineData(150, 100)]
    [InlineData(25, 25)]
    public void PagedRequestContrato_deve_ajustar_pageSize_para_limites(int inputSize, int expectedSize)
    {
        var req = new PagedRequestContrato(1, inputSize);
        req.PageSize.Should().Be(expectedSize);
    }

    [Fact]
    public void PagedRequestContrato_deve_calcular_skip_corretamente()
    {
        var req = new PagedRequestContrato(page: 3, pageSize: 20);
        req.Skip.Should().Be(40);
    }

    [Fact]
    public void PagedResultContrato_deve_calcular_total_pages_e_flags_de_navegacao()
    {
        var items = new List<string> { "item1", "item2", "item3" };
        var result = new PagedResultContrato<string>(items, page: 2, pageSize: 3, totalItems: 10);

        result.Items.Should().HaveCount(3);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(3);
        result.TotalItems.Should().Be(10);
        result.TotalPages.Should().Be(4); // ceil(10/3) = 4
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PagedResultContrato_primeira_e_ultima_pagina_devem_atualizar_flags()
    {
        var firstPage = new PagedResultContrato<int>([1, 2], page: 1, pageSize: 2, totalItems: 4);
        firstPage.HasPreviousPage.Should().BeFalse();
        firstPage.HasNextPage.Should().BeTrue();

        var lastPage = new PagedResultContrato<int>([3, 4], page: 2, pageSize: 2, totalItems: 4);
        lastPage.HasPreviousPage.Should().BeTrue();
        lastPage.HasNextPage.Should().BeFalse();
    }
}
