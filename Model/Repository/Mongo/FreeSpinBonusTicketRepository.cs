using Tool.Model.Entity.Mongo;
using MongoDB.Driver;

namespace Tool.Model.Repository.Mongo;

/// <summary>
/// 贈送免費旋轉票券 Repository
/// </summary>
public class FreeSpinBonusTicketRepository(IMongoDatabase mongoDbContext) : MongoRepository<FreeSpinBonusTicket>(mongoDbContext), IFreeSpinBonusTicketRepository
{
}


/// <summary>
/// 贈送免費旋轉票券 Repository
/// </summary>
public interface IFreeSpinBonusTicketRepository : IMongoRepository<FreeSpinBonusTicket>
{
}