namespace CivOne.Persistence.Mapper
{
    public interface DtoMapper<TDto, TDomain>
    {
        TDomain FromDto(TDto dto);
        TDto ToDto(TDomain domain);
    }
}