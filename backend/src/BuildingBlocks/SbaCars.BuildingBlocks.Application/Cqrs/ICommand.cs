namespace SbaCars.BuildingBlocks.Application.Cqrs;

/// <summary>Marker for an application operation that changes state.</summary>
public interface ICommand<TResult>;

/// <summary>Marker for a read-only application operation.</summary>
public interface IQuery<TResult>;
