using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.DAL;
using Gatewing.ProductionTools.Logging;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NHibernate;
using Ninject;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Constants
{
    public class Strings
    {
        public const string AnimationClasses = "nga-fast nga-stagger-fast nga-fade-all nga-slide-right-all";
    }
}

namespace Gatewing.ProductionTools.GTS.Controllers
{
    public class AuthorizeUsersInRoleAttribute : AuthorizeAttribute
    {
        private string _roleName;

        public AuthorizeUsersInRoleAttribute(string roleName)
        {
            _roleName = roleName;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (httpContext.User.IsInRole(_roleName))
                return true;

            return base.AuthorizeCore(httpContext);
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="System.Web.Mvc.Controller" />
    public class BaseController : Controller
    {
        /// <summary>
        /// The logger helper
        /// </summary>
        internal Logger _logger = new Logger("Ops Portal");

        internal string GuidRegEx = @"^[{(]?[0-9a-fA-F]{8}[-]?([0-9a-fA-F]{4}[-]?){3}[0-9a-fA-F]{12}[)}]?$";

        /// <summary>
        /// Gets or sets the GTS repository.
        /// </summary>
        /// <value>
        /// The GTS repository.
        /// </value>
        [Inject]
        public IGTSDataRepository GTSDataRepository { get; set; }

        /// <summary>
        /// Gets or sets the unit of work.
        /// </summary>
        /// <value>
        /// The unit of work.
        /// </value>
        [Inject]
        public IUnitOfWork UnitOfWork { get; set; }

        /// <summary>
        /// Gets the unit of work session.
        /// </summary>
        /// <value>
        /// The unit of work nh session.
        /// </value>
        public ISession UnitOfWorkSession { get { return ((UnitOfWork)UnitOfWork).Session; } }

        /// <summary>
        /// Tracks the addition of an object to a collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="addedObject">The added object.</param>
        /// <param name="parentObject">The parent object.</param>
        /// <param name="propName">Name of the property.</param>
        public void TrackAddition<T, U>(T addedObject, U parentObject, string propName) where T : DomainObject where U : DomainObject
        {
            var change = new EntityChange
            {
                Id = Guid.NewGuid(),
                ChangedEntityId = parentObject.Id,
                ChangedEntityType = typeof(U),
                ChangeDate = DateTime.Now,
                //ChangeDescription = string.Format("Property '{0}' is changed from '{1}' to '{2}'", prop.Name, oldValue, newValue),
                ChangeMadeBy = addedObject.EditedBy,
                OldValue = "Parent Id: " + parentObject.Id.ToString(),
                NewValue = "Added object Id: " + addedObject.Id.ToString(),
            };

            switch (typeof(T).Name)
            {
                case "ProductComponent":
                    var model = parentObject as ProductModel;
                    var component = addedObject as ProductComponent;
                    change.ChangeDescription = string.Format("Model '{0}' with has added Component '{1}' to its collection of ProductComponents", model.Name, component.ComponentName);
                    break;

                case "AssemblyTool":
                    model = parentObject as ProductModel;
                    var tool = addedObject as AssemblyTool;
                    change.ChangeDescription = string.Format("Model '{0}' with has added Tool '{1}' to its collection of Tools", model.Name, tool.Name + " " + tool.ToolCode);
                    break;

                case "GTSWorkInstruction":
                    var parentComponent = parentObject as ProductComponent;
                    var instruction = addedObject as GTSWorkInstruction;
                    change.ChangeDescription = string.Format("Product Component '{0}' with has added Work Instruction '{1}' to its collection of WorkInstructions", parentComponent.ComponentName, instruction.Title);
                    break;
            }

            // save change to system
            GTSDataRepository.Create<EntityChange>(change);
        }

        /// <summary>
        /// Attempts to tracks changes to an entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="newEntity">The new entity.</param>
        public void TrackChanges<T>(T newEntity) where T : DomainObject
        {
            var type = newEntity.GetType();
            var entity = GTSDataRepository.GetById<T>(newEntity.Id);
            TrackChanges<T>(entity, newEntity);
        }

        /// <summary>
        /// Attempts to tracks changes to an entity.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="oldEntity">The old entity.</param>
        /// <param name="newEntity">The new entity.</param>
        public void TrackChanges<T>(T oldEntity, T newEntity) where T : DomainObject
        {
            // get a list of properties of the entity
            var props = typeof(T).GetProperties().ToList();

            var name = string.Empty;
            switch (typeof(T).Name)
            {
                case "ProductModel":
                    name = (newEntity as ProductModel).Name;
                    break;

                case "ProductComponent":
                    name = (newEntity as ProductComponent).ComponentName;
                    break;

                case "GTSWorkInstruction":
                    name = (newEntity as GTSWorkInstruction).Title;
                    break;
            }

            // iterate over all properties and track changes
            props.ForEach(prop =>
            {
                // get values
                var oldValue = prop.GetValue(oldEntity);
                var newValue = prop.GetValue(newEntity);

                // check for differences
                if ((oldValue != null && newValue != null)
                    && !oldValue.Equals(newValue)
                    && !(oldValue is IList) && !oldValue.GetType().IsGenericType
                    && prop.Name != "ModificationDate")
                {
                    // create and save new EntityChange object
                    var change = new EntityChange
                    {
                        Id = Guid.NewGuid(),
                        ChangedEntityId = oldEntity.Id,
                        ChangedEntityType = typeof(T),
                        ChangeDate = DateTime.Now,
                        ChangeDescription = string.Format("{3} '{4}'s property '{0}' is changed from '{1}' to '{2}'", prop.Name, oldValue, newValue, typeof(T).Name, name),
                        ChangeMadeBy = User.Identity.Name, //oldEntity.EditedBy,
                        OldValue = oldValue.ToString(),
                        NewValue = newValue.ToString()
                    };

                    // save change to system
                    GTSDataRepository.Create<EntityChange>(change);
                }
            });
        }

        /// <summary>
        /// Tracks the property changes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="propName">Name of the property.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        public void TrackProperty<T>(T entity, string propName, string oldValue, string newValue) where T : DomainObject
        {
            var name = typeof(T).Name;
            switch (typeof(T).Name)
            {
                case "ProductModel":
                    name = (entity as ProductModel).Name;
                    break;

                case "ProductComponent":
                    name = (entity as ProductComponent).ComponentName;
                    break;

                case "WorkInstruction":
                    name = (entity as GTSWorkInstruction).Title;
                    break;
            }

            var change = new EntityChange
            {
                Id = Guid.NewGuid(),
                ChangedEntityId = entity.Id,
                ChangedEntityType = typeof(T),
                ChangeDate = DateTime.Now,
                ChangeDescription = string.Format("{3} '{4}'s property '{0}' was changed from '{1}' to '{2}'.", propName, oldValue, newValue, typeof(T).Name, name),
                ChangeMadeBy = entity.EditedBy,
                OldValue = oldValue,
                NewValue = newValue
            };

            // save change to system
            GTSDataRepository.Create<EntityChange>(change);
        }

        /// <summary>
        /// Tracks the removal of an object from a collection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="removedObject">The added object.</param>
        /// <param name="parentObject">The parent object.</param>
        /// <param name="propName">Name of the property.</param>
        public void TrackRemoval<T, U>(T removedObject, U parentObject, string propName) where T : DomainObject where U : DomainObject
        {
            var change = new EntityChange
            {
                Id = Guid.NewGuid(),
                ChangedEntityId = parentObject.Id,
                ChangedEntityType = typeof(U),
                ChangeDate = DateTime.Now,
                //ChangeDescription = string.Format("Property '{0}' is changed from '{1}' to '{2}'", prop.Name, oldValue, newValue),
                ChangeMadeBy = removedObject.EditedBy,
                OldValue = "Parent Id: " + parentObject.Id.ToString(),
                NewValue = "Removed object id: " + removedObject.Id.ToString()
            };

            switch (typeof(T).Name)
            {
                case "ProductComponent":
                    var model = parentObject as ProductModel;
                    var component = removedObject as ProductComponent;
                    change.ChangeDescription = string.Format("Model '{0}' with has removed Component '{1}' from its collection of ProductComponents by archiving the component.", model.Name, component.ComponentName);
                    break;

                case "AssemblyTool":
                    model = parentObject as ProductModel;
                    var tool = removedObject as AssemblyTool;
                    change.ChangeDescription = string.Format("Model '{0}' has removed Tool '{1}' from its collection of Tools", model.Name, tool.Name + " " + tool.ToolCode);
                    break;

                case "GTSWorkInstruction":
                    var parentComponent = parentObject as ProductComponent;
                    var instruction = removedObject as GTSWorkInstruction;
                    change.ChangeDescription = string.Format("Product Component '{0}' with has removed Work Instruction '{1}' from its collection of WorkInstructions", parentComponent.ComponentName, instruction.Title);
                    break;
            }

            // save change to system
            GTSDataRepository.Create<EntityChange>(change);
        }

        /// <summary>
        /// Gets the open remarks (not archived, not resolved) for a particular assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns></returns>
        internal List<RemarkSymptom> GetOpenRemarksForAssembly(Guid assemblyId)
        {
            return GTSDataRepository.GetListByQuery<RemarkSymptom>("FROM RemarkSymptom rs WHERE rs.IsArchived = false AND rs.Resolved = false AND rs.ProductAssembly.Id = :id",
                new Dictionary<string, object> { { "id", assemblyId } }).ToList();
        }

        /// <summary>
        /// Serializes for web.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        internal ActionResult SerializeForWeb(object entity)
        {
            return Json(
                JsonConvert.SerializeObject(
                    entity,
                    Formatting.None,
                    new JsonSerializerSettings() { /*ContractResolver = new NHibernateContractResolver(),*/ ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore }),
                JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Called when [action executing].
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!filterContext.IsChildAction)
                UnitOfWork.BeginTransaction();
        }

        /// <summary>
        /// Called when [result executed].
        /// </summary>
        /// <param name="filterContext">The filter context.</param>
        protected override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (!filterContext.IsChildAction)
                UnitOfWork.Commit();
        }

        /*
        public class NHibernateContractResolver : DefaultContractResolver
        {
            protected override JsonContract CreateContract(Type objectType)
            {
                if (typeof(NHibernate.Proxy.INHibernateProxy).IsAssignableFrom(objectType))
                    return base.CreateContract(objectType.BaseType);
                else
                    return base.CreateContract(objectType);
            }
        }*/
    }
}