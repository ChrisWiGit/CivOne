namespace CivOne.Persistence.Model
{
    public interface DtoMapper<TDto, TDomain>
    {
        TDomain FromDto(TDto dto);
        TDto ToDto(TDomain domain);
    }
}