using Gatewing.ProductionTools.BLL;
using Gatewing.ProductionTools.Logging;
using NHibernate.Event;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gatewing.ProductionTools.DAL.EventListeners
{
    public abstract class BaseBusinessRuleEventListener
    {
        public void DoCheck(object entityAsObject)
        {
            var domainObject = (DomainObject)entityAsObject;

            var _logger = new Logger("BusinessRules");

            try
            {
                var _gtsRepository = new GTSDataRepository(new UnitOfWork(), _logger); //.Instance(true);
                // var currentSerial = string.Empty;
                // check if a referenced product in a component isn't used anywhere else

                //if (domainObject.GetType() == typeof(ComponentAssembly))
                //{
                //    var entity = (ComponentAssembly)domainObject;
                //    if (entity.ProductComponent.UnderlyingProductModel != null)
                //    {
                //        if (!string.IsNullOrEmpty(entity.Serial))
                //        {
                //            // get all ComponentAssemblies that use the current serial
                //            var usedSerialInComponents = _gtsRepository.GetListByQuery<ComponentAssembly>("from ComponentAssembly ca WHERE ca.Serial = :serial AND ca.Id <> :id AND ca.ProductComponent.UnderlyingProductModel.BaseModelId = :baseModelId",
                //                new Dictionary<string, object>() {
                //                { "serial", entity.Serial },
                //                { "id", entity.Id },
                //                { "baseModelId", entity.ProductComponent.UnderlyingProductModel.BaseModelId}
                //                });

                //            // serial is found in other components / models => throw error
                //            if (usedSerialInComponents.Count() > 0 && currentSerial != entity.Serial)
                //            {
                //                currentSerial = entity.Serial;
                //                //entity.Serial = string.Empty;
                //                //_gtsRepository.Update<ComponentAssembly>(entity);
                //                throw new InvalidOperationException("The component (" + entity.ProductComponent.ComponentName + ") that references a product with serial (" + currentSerial + ") is already used in another product assembly.");
                //            }

                //            // new serial entered, check if we need to auto create referenced model
                //            if (usedSerialInComponents.Count() == 0 && entity.Serial != null && entity.ProductAssembly.ProductSerial != entity.Serial)
                //            {
                //                if (entity.ProductComponent.UnderlyingProductModel.AutoCreate)
                //                {/*
                //                    var existingAssemblies = _gtsRepository.GetListByQuery<ProductAssembly>("FROM ProductAssembly Where ProductSerial = :serial AND Id != :id",
                //                        new Dictionary<string, object> { { "serial", entity.Serial }, { "id", entity.ProductAssembly.Id } }).ToList();

                //                    var createNew = true;
                //                    if (existingAssemblies.Count() > 0)
                //                    {
                //                        existingAssemblies.ForEach(currentAssembly =>
                //                        {
                //                            if (currentAssembly.ProductSerial != entity.Serial)
                //                                throw new InvalidOperationException("This serial is already taken for this " + entity.ProductComponent.UnderlyingProductModel.Name);
                //                            else
                //                                createNew = false;
                //                        });
                //                    }

                //                    if (createNew)
                //                    {
                //                        var datetimeNow = DateTime.Now;
                //                        var assembly = new ProductAssembly();
                //                        assembly.Id = Guid.NewGuid();
                //                        assembly.StartDate = datetimeNow;
                //                        assembly.StartedBy = ((ProductAssembly)assembly).EditedBy = entity.EditedBy;
                //                        assembly.EndDate = datetimeNow.AddMinutes(1);
                //                        assembly.ProductModel = entity.ProductComponent.UnderlyingProductModel;
                //                        assembly.ProductModelState = _gtsRepository.GetListByQuery<ProductModelState>("FROM ProductModelState Where Name = 'Delivered'", null).FirstOrDefault();
                //                        assembly.ProductSerial = entity.Serial;
                //                        assembly.NHVersion = 1;

                //                        var tmp = new UnitOfWork();
                //                        tmp.Session.Save(assembly);

                //                        //_gtsRepository.Create<ProductAssembly>(assembly);
                //                    }*/
                //                }
                //                else
                //                {
                //                    var existingAssembly = _gtsRepository.GetListByQuery<ProductAssembly>("FROM ProductAssembly Where ProductSerial = :serial AND Id != :id",
                //                        new Dictionary<string, object> { { "serial", entity.Serial }, { "id", entity.ProductAssembly.Id } }).FirstOrDefault();

                //                    if (existingAssembly == null)
                //                    {
                //                        var serial = entity.Serial;
                //                        //entity.Serial = string.Empty;
                //                        //_gtsRepository.Save<Compon>(assembly);
                //                        throw new InvalidOperationException("You are trying to reference a product with serial (" + serial + ") that doesn't exist.");
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}

                // check if a user attempts to resolve a remark of type empty
                if (domainObject.GetType() == typeof(RemarkSymptom))
                {
                    var entity = (RemarkSymptom)domainObject;
                    if (entity.Resolved && entity.RemarkSymptomType.Name == "Empty")
                    {
                        //entity.Resolved = false;
                        throw new InvalidOperationException("You cannot resolve a remark of type 'Empty'.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex);
                throw;
            }
        }
    }

    /// <summary>
    /// This class is meant to enforce business rules. This does not mean that all persistance operations are only checked here. Some (such as model release) have concurrency checks in the method bodies themselves.
    /// </summary>
    /// <seealso cref="NHibernate.Event.IPreUpdateEventListener" />
    public class BusinessRulePreUpdateEventListener : IPreUpdateEventListener
    {
        public bool OnPreUpdate(PreUpdateEvent @event)
        {
            if (@event.Entity.GetType() == typeof(RemarkSymptomType) && @event.OldState[0].ToString().ToLower() == "salvage")
                EnforceLockedTypesAndStates(@event, "salvage");

            if ((@event.Entity.GetType() == typeof(RemarkSymptomType) || @event.Entity.GetType() == typeof(RemarkSymptomCauseType) || @event.Entity.GetType() == typeof(RemarkSymptomSolutionType)) && @event.OldState[0].ToString().ToLower() == "empty")
                EnforceLockedTypesAndStates(@event, "empty");

            if (@event.Entity.GetType() == typeof(RemarkSymptomCauseType) && @event.OldState[0].ToString().ToLower() == "salvageable component" && @event.OldState[0].ToString() != @event.State[0].ToString())
                EnforceLockedTypesAndStates(@event, "salvageable component");

            if (@event.Entity.GetType() == typeof(RemarkSymptomSolutionType) && @event.OldState[0].ToString().ToLower() == "salvaged" && @event.OldState[0].ToString() != @event.State[0].ToString())
                EnforceLockedTypesAndStates(@event, "salvaged");

            if (@event.Entity.GetType() == typeof(RemarkSymptomSolutionType) && @event.OldState[0].ToString().ToLower() == "replace" && @event.OldState[0].ToString() != @event.State[0].ToString())
                EnforceLockedTypesAndStates(@event, "replace");

            if (@event.Entity.GetType() == typeof(ProductModelState) && @event.OldState[0].ToString().ToLower() == "delivered" && @event.OldState[0].ToString() != @event.State[0].ToString())
                EnforceLockedTypesAndStates(@event, "delivered");

            if (@event.Entity.GetType() == typeof(ProductModelState) && @event.OldState[0].ToString().ToLower() == "scrapped" && @event.OldState[0].ToString() != @event.State[0].ToString())
                EnforceLockedTypesAndStates(@event, "scrapped");

            return false;
        }

        private void EnforceLockedTypesAndStates(PreUpdateEvent @event, string value)
        {
            if (@event.OldState[0].ToString() != @event.State[0].ToString())
                throw new InvalidOperationException("You cannot change the state " + value);

            if (((ArchivableDomainObject)@event.Entity).IsArchived)
                throw new InvalidOperationException("You cannot archive this particular state or type: " + value);
        }
    }

    public class BusinessRuleSaveOrUpdateEventListener : BaseBusinessRuleEventListener, ISaveOrUpdateEventListener
    {
        private IGTSDataRepository _gtsRepository;
        private Logger _logger;

        public BusinessRuleSaveOrUpdateEventListener(IGTSDataRepository gtsRepository, Logger logger)
        {
            _logger = logger;
            _gtsRepository = gtsRepository;
        }

        public BusinessRuleSaveOrUpdateEventListener()
        {
        }

        public void OnSaveOrUpdate(SaveOrUpdateEvent @event)
        {
            DoCheck(@event.Entity);
        }
    }
}