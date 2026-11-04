namespace Fiap.TechChallenge.Contracts.Common;

/// <summary>
/// Contrato padrão para requisições paginadas nos microsserviços.
/// </summary>
public record PagedRequestContrato
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 10;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        init => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value < 1 ? DefaultPageSize : (value > MaxPageSize ? MaxPageSize : value);
    }

    public int Skip => (Page - 1) * PageSize;

    public PagedRequestContrato(int page = 1, int pageSize = DefaultPageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public PagedRequestContrato() : this(1, DefaultPageSize) { }
}

/// <summary>
/// Contrato padrão para respostas paginadas nos microsserviços.
/// </summary>
public record PagedResultContrato<T>
{
    public IReadOnlyCollection<T> Items { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalItems { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalItems / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResultContrato(IReadOnlyCollection<T> items, int page, int pageSize, int totalItems)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
    }
}
