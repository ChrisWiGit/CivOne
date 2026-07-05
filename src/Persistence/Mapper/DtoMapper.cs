namespace CivOne.Persistence.Mapper
{
    public interface IDtoMapper<TDto, TDomain>
    {
        TDomain FromDto(TDto dto);
        TDto ToDto(TDomain domain);
    }
}