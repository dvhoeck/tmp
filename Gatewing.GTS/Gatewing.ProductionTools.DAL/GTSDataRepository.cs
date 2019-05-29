using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.Logging;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
///
/// </summary>
namespace Gatewing.ProductionTools.DAL
{
    public class GTSDataRepository : IGTSDataRepository
    {
        private Logger _logger;
        private UnitOfWork _unitOfWork;

        public GTSDataRepository(IUnitOfWork unitOfWork, Logger logger)
        {
            _unitOfWork = (UnitOfWork)unitOfWork;
            _logger = logger;
        }

        protected ISession Session { get { return _unitOfWork.Session; } }

        /// <summary>
        /// Archives product models by their base model identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="username">The username.</param>
        public void ArchiveByBaseModelId<T>(Guid id, string username) where T : ArchivableDomainObject
        {
            _logger.LogDebug("User {1} attempts to logically delete all entities with base model id: {0}", id, username);
            ExecuteUpdateQuery("Update ProductModel SET IsArchived = true, ArchivedBy = :name, ArchivalDate = :dateNow where BaseModelId = :id",
                new Dictionary<string, object> { { "id", id }, { "name", username }, { "dateNow", DateTime.Now } });
        }

        /// <summary>
        /// Archives an entity by its identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="username">The username.</param>
        public void ArchiveById<T>(Guid id, string username) where T : ArchivableDomainObject
        {
            _logger.LogDebug("User {2} attempts to archive an {0} entity with id: {1}", typeof(T), id, username);

            var entity = Session.Get<T>(id);
            entity.IsArchived = true;
            entity.ArchivedBy = username;
            entity.ArchivalDate = DateTime.Now;
            Session.Update(entity);
        }

        public void Create<T>(T entity)
        {
            Session.Save(entity);
        }

        public void CreateAll<T>(IEnumerable<T> entities)
        {
            entities.ForEach(x => Session.Save(x));
        }

        public void Delete<T>(Guid id)
        {
            Session.Delete(Session.Load<T>(id));
        }

        /// <summary>
        /// Deletes an entity by its identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The identifier.</param>
        public virtual void DeleteById<T>(Guid id) where T : DomainObject
        {
            _logger.LogDebug("Attempting to delete entity of type {0} with id {1} ", typeof(T), id);
            var queryString = string.Format("delete {0} where id = :id", typeof(T));
            Session.CreateQuery(queryString)
                        .SetParameter("id", id)
                        .ExecuteUpdate();
        }

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="doSqlStament">if set to <c>true</c> [do SQL stament].</param>
        /// <returns></returns>
        public int ExecuteQuery(string query, IList<KeyValuePair<string, object>> parameters, bool doSqlStament = false)
        {
            var result = -1;
            _logger.LogDebug("Attempting to execute query [{0}]", query);

            IQuery nHQuery = null;
            if (doSqlStament)
                nHQuery = Session.CreateSQLQuery(query);
            else
                nHQuery = Session.CreateQuery(query);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    nHQuery.SetParameter(param.Key, param.Value);
                }
            }

            result = Convert.ToInt32(nHQuery.UniqueResult());

            return result;
        }

        /// <summary>
        /// Executes a query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="doSqlStament">if set to <c>true</c> [do SQL stament].</param>
        /// <returns></returns>
        public virtual int ExecuteUpdateQuery(string query, Dictionary<string, object> parameters, bool doSqlStament = false)
        {
            var result = -1;
            _logger.LogDebug("Attempting to execute query [{0}]", query);

            IQuery nHQuery = null;
            if (doSqlStament)
                nHQuery = Session.CreateSQLQuery(query);
            else
                nHQuery = Session.CreateQuery(query);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    nHQuery.SetParameter(param.Key, param.Value);
                }
            }

            result = nHQuery.ExecuteUpdate();

            return result;
        }

        public IQueryable<T> GetAll<T>()
        {
            return Session.Query<T>();
        }

        public T GetById<T>(Guid id)
        {
            return Session.Get<T>(id);
        }

        public T GetByProductSerial<T>(string productSerial, Guid baseModelId) where T : class
        {
            var criteria = Session.CreateCriteria<T>();
            criteria = criteria.Add(Expression.Eq("ProductSerial", productSerial));
            criteria = criteria.CreateCriteria("ProductModel");
            criteria = criteria.Add(Expression.Eq("BaseModelId", baseModelId));

            return criteria.UniqueResult<T>();
        }

        /// <summary>
        /// Gets the list by query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public IEnumerable<T> GetListByQuery<T>(string query, Dictionary<string, object> parameters)
        {
            _logger.LogDebug("Attempting to get a list of entities by executing a query.");

            var nHQuery = Session.CreateQuery(query);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    nHQuery.SetParameter(param.Key, param.Value);
                }
            }

            return nHQuery.List<T>();
        }

        public void Update<T>(T entity)
        {
            Session.Update(entity);
        }

        public void UpdateAll<T>(IEnumerable<T> entities)
        {
            entities.ForEach(x => Session.Update(x));
        }
    }
}