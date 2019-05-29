using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.BLL
{
    public interface IGTSDataRepository
    {
        void ArchiveByBaseModelId<T>(Guid id, string username) where T : ArchivableDomainObject;

        void ArchiveById<T>(Guid id, string userName) where T : ArchivableDomainObject;

        void Create<T>(T entity);

        void CreateAll<T>(IEnumerable<T> entities);

        void Delete<T>(Guid id);

        void DeleteById<T>(Guid id) where T : DomainObject;

        int ExecuteQuery(string query, IList<KeyValuePair<string, object>> parameters, bool doSqlStament = false);

        int ExecuteUpdateQuery(string query, Dictionary<string, object> parameters, bool doSqlStament = false);

        IQueryable<T> GetAll<T>();

        T GetById<T>(Guid id);

        T GetByProductSerial<T>(string productSerial, Guid baseModelId) where T : class;

        IEnumerable<T> GetListByQuery<T>(string query, Dictionary<string, object> parameters = null);

        void Update<T>(T entity);

        void UpdateAll<T>(IEnumerable<T> entities);
    }
}