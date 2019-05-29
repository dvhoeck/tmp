using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.DAL.EventListeners;
using Gatewing.ProductionTools.Logging;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Event;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Gatewing.ProductionTools.DAL
{
    public static class QueriesExtentions
    {
        public static IQueryOver<E, F> WhereStringIsNotNullOrEmpty<E, F>(this IQueryOver<E, F> query, Expression<Func<E, object>> propExpression)
        {
            var prop = Projections.Property(propExpression);
            var criteria = Restrictions.Or(Restrictions.IsNull(prop), Restrictions.Eq(Projections.SqlFunction("trim", NHibernateUtil.String, prop), ""));
            return query.Where(Restrictions.Not(criteria));
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private static readonly ISessionFactory _sessionFactory;
        private static Logger _logger;
        private ITransaction _transaction;

        static UnitOfWork()
        {
            var rootWebConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/");
            var connectionString = rootWebConfig.ConnectionStrings.ConnectionStrings["GTSDB"].ConnectionString;
            var connection = MsSqlConfiguration.MsSql2012.ShowSql().ConnectionString(connectionString);

            ISaveOrUpdateEventListener[] stack = new ISaveOrUpdateEventListener[] { new BusinessRuleSaveOrUpdateEventListener(), new NHibernate.Event.Default.DefaultSaveOrUpdateEventListener() };
            IPreUpdateEventListener[] preUpdateStack = new IPreUpdateEventListener[] { new BusinessRulePreUpdateEventListener() };

            // Initialise singleton instance of ISessionFactory, static constructors are only executed once during the
            // application lifetime - the first time the UnitOfWork class is used
            _sessionFactory = Fluently.Configure()
                .Database(connection)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<UnitOfWork>()) // load all mappings from this namespace
                .ExposeConfiguration(config =>
                {
                    new SchemaUpdate(config).Execute(false, true);

                    config.EventListeners.SaveEventListeners = stack;
                    config.EventListeners.SaveOrUpdateEventListeners = stack;
                    config.EventListeners.PreUpdateEventListeners = preUpdateStack;
                    config.EventListeners.UpdateEventListeners = stack;
                })
                .BuildSessionFactory();

            _logger = new Logger("UnitOfWork");
            _logger.LogInfo("Using connection string: " + connectionString);
        }

        public UnitOfWork()
        {
            Session = _sessionFactory.OpenSession();
        }

        public ISession Session { get; private set; }

        public void BeginTransaction()
        {
            _transaction = Session.BeginTransaction();
        }

        public void Commit()
        {
            try
            {
                // commit transaction if there is one active
                if (_transaction != null && _transaction.IsActive)
                    _transaction.Commit();
            }
            catch (Exception ex)
            {
                // rollback if there was an exception
                if (_transaction != null && _transaction.IsActive)
                    _transaction.Rollback();

                _logger.LogError("Could not commit transaction due to error.");
                _logger.LogError(ex);

                throw;
            }
            finally
            {
                Session.Dispose();
            }
        }

        public void Rollback()
        {
            try
            {
                if (_transaction != null && _transaction.IsActive)
                    _transaction.Rollback();
            }
            finally
            {
                Session.Dispose();
            }
        }
    }
}