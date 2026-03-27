using Tool.Entity;
using Tool.Model.Entity.MySQL;

namespace Tool.Model.Repository.FiveGame;

public class FiveGameRepository<TEntity> : BaseRepository<TEntity>, IRepository<TEntity> where TEntity : class
{
    public FiveGameRepository(FiveGameEntities context) : base(context)
    {
        _context = context;
    }
}