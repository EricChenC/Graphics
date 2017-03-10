using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.VFX
{
    [Serializable]
    class VFXSlot : VFXModel<VFXSlot, VFXSlot>
    {
        public enum Direction
        {
            kInput,
            kOutput,
        }

        public Direction direction { get { return m_Direction; } }
        public VFXProperty property { get { return m_Property; } }
        public override string name { get { return m_Property.name; } }

        public virtual VFXExpression expression 
        { 
            get
            {
                return m_CurrentExpression;
            }
        }

        public void SetExpression(VFXExpression expression,bool notify = true)
        {
            if (m_CurrentExpression != expression)
            {
                if (!CanConvert(expression))
                {
                    throw new Exception();
                }

                m_CurrentExpression = ConvertExpression(expression);
                if (notify)
                    Invalidate(InvalidationCause.kConnectionChanged); // trigger a rebuild
            }
        }

        public ReadOnlyCollection<VFXSlot> LinkedSlots
        {
            get
            {
                return m_LinkedSlots.AsReadOnly();
            }
        }

        public VFXSlot refSlot
        { 
            get 
            {
                if (direction == Direction.kOutput || !HasLink())
                    return this;
                return m_LinkedSlots[0];
            } 
        }

        public IVFXSlotContainer owner { get { return m_Owner; } }

        protected VFXSlot() {} // For serialization only
        public VFXSlot(Direction direction)
        {
            m_Direction = direction;
        }

        // Create and return a slot hierarchy from a property info
        public static VFXSlot Create(VFXProperty property, Direction direction, VFXExpression defaultExpression = null)
        {
            var desc = VFXLibrary.GetSlot(property.type);
            if (desc != null)
            {
                var slot = desc.CreateInstance();
                slot.m_Direction = direction;
                slot.m_Property = property;
                slot.m_DefaultExpression = defaultExpression;
                slot.m_CurrentExpression = slot.m_DefaultExpression;

                foreach (var subInfo in property.SubProperties())
                {
                    var subSlot = Create(subInfo, direction, null /* TODO : transform base expression to subproperty */);
                    if (subSlot != null)
                        subSlot.Attach(slot,false);
                }

                return slot;
            }

            throw new InvalidOperationException(string.Format("Unable to create slot for property {0} of type {1}",property.name,property.type));
        }
    
        public int GetNbLinks() { return m_LinkedSlots.Count; }
        public bool HasLink() { return GetNbLinks() != 0; }
        
        public bool CanLink(VFXSlot other)
        {
            return direction != other.direction && !m_LinkedSlots.Contains(other) && CanConvert(other.expression);
        }

        public bool Link(VFXSlot other, bool notify = true)
        {
            if (other == null)
                return false;

            if (!CanLink(other) || !other.CanLink(this)) // can link
                return false;

            if (other.direction == Direction.kInput)
            {
                InnerLink(other, notify);
                other.InnerLink(this, notify);
            }
            else
            {
                other.InnerLink(this, notify);
                InnerLink(other, notify);
            }

            return true;
        }

        public void Unlink(VFXSlot other, bool notify = true)
        {
            if (m_LinkedSlots.Contains(other))
            {
                InnerUnlink(other,notify);
                other.InnerUnlink(this,notify);
            }
        }

        protected void PropagateToOwner(Action<IVFXSlotContainer> func)
        {
            if (m_Owner != null)
                func(m_Owner);
            else
            {
                var parent = GetParent();
                if (parent != null)
                    parent.PropagateToOwner(func);
            }
        }

        protected void PropagateToParent(Action<VFXSlot> func)
        {
            var parent = GetParent();
            if (parent != null)
            {
                func(parent);
                PropagateToParent(func);   
            }
        }

        protected void PropagateToChildren(Action<VFXSlot> func)
        {
            foreach (var child in children)
            {
                func(child);
                PropagateToChildren(func);
            }
        }

        protected void PropagateToTree(Action<VFXSlot> func)
        {
            PropagateToParent(func);
            PropagateToChildren(func);
        }

        private void SetAndPropagateExpression(VFXExpression expression)
        {
            SetExpression(refSlot.expression, false);
            PropagateToParent(s => s.m_CurrentExpression = s.ExpressionFromChildren());
            PropagateToChildren(s => s.m_CurrentExpression = s.GetParent().ExpressionToChildren()[0]);
            PropagateToOwner(o => o.Invalidate(VFXModel.InvalidationCause.kConnectionChanged)); // TODO Needs an invalidate with model passed   
        }

        private void ConnectInput(VFXSlot slot)
        {
            UnlinkAll(false); // First disconnect any other linked slot
            PropagateToTree(s => s.UnlinkAll(false)); // First unlink other links in tree
            SetAndPropagateExpression(ConvertExpression(slot.expression));
        }

        private void DisconnectInput()
        {
            SetAndPropagateExpression(m_DefaultExpression); // Set the default expression
        }

        private void ConnectOutput(VFXSlot slot)
        {
            // Nothing
        }

        private void DisconnectOutput()
        {
            // Nothing
        }

        public void UnlinkAll(bool notify = true)
        {
            var currentSlots = new List<VFXSlot>(m_LinkedSlots);
            foreach (var slot in currentSlots)
                Unlink(slot,notify);
        }

        private void InnerLink(VFXSlot other,bool notify)
        {
            // inputs can only be linked to one output at a time
            /*if (direction == Direction.kInput)
            {
                UnlinkAll(notify);

                // We need to unlink any potential slots link in the hierarchy
                var currentParent = GetParent();
                while (currentParent != null)
                {
                    currentParent.UnlinkAll();
                    currentParent = currentParent.GetParent();
                }

                foreach (var child in children)
                    child.UnlinkAll(notify);
            }*/



            
            m_LinkedSlots.Add(other);
            if (direction == Direction.kInput)
                ConnectInput(other);

            //if (notify)
            //    Invalidate(InvalidationCause.kConnectionChanged);
        }

        private void InnerUnlink(VFXSlot other, bool notify)
        {
            if (m_LinkedSlots.Remove(other) && notify)
                Invalidate(InvalidationCause.kConnectionChanged);
        }

        protected virtual void Invalidate(VFXModel model,InvalidationCause cause)
        {
            // Dont call the base invalidate as we dont want to propagate to parent systematically

            if (direction == Direction.kInput)
            {

            }

        }





        /*protected override void Invalidate(VFXModel model, InvalidationCause cause)
        {
            base.Invalidate(model, cause);

            // Propagate to the owner if any
            if (direction == Direction.kInput)
            {
                if (HasLink())
                {
                    if (!CanConvert(refSlot.expression))
                    {
                        //Invalid link, disconnect it
                        UnlinkAll();
                    }
                    else
                    {
                        //Reapply expression
                        SetExpression(refSlot.expression,false);
                    }
                }
                else
                {
                    //No link anymore, default fallback
                    SetExpression(m_DefaultExpression,false);
                }

                if (m_Owner != null)
                {
                    m_Owner.Invalidate(cause);
                }
                
            }
            else
            {
                foreach (var link in m_LinkedSlots)
                {
                    link.Invalidate(cause);
                }
            }
        }*/

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            /*if (direction == Direction.kOutput)
            {
                var linkedSlot = m_LinkedSlots.ToArray();
                if (cause == InvalidationCause.kConnectionChanged) // If connection has changed, propagate to linked inputs
                    foreach (var slot in linkedSlot)
                        slot.Invalidate(model, InvalidationCause.kConnectionPropagated);

                if (cause == InvalidationCause.kParamChanged) // If param has changed, propagate to linked inputs
                    foreach (var slot in linkedSlot)
                        slot.Invalidate(model, InvalidationCause.kParamPropagated);
            }
            else*/ // input
            {
                BuildExpression();
            }
        }

        private VFXExpression BuildExpression()
        {
            return null;
        }

        protected virtual bool CanConvert(VFXExpression expression)
        {
            return m_DefaultExpression == null || expression == null || m_DefaultExpression.ValueType == expression.ValueType;
        }
        protected virtual VFXExpression ConvertExpression(VFXExpression expression)
        {
            return expression;
        }

        protected virtual VFXExpression[] ExpressionToChildren()    { return null; }
        protected virtual VFXExpression ExpressionFromChildren()    { return null; }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            m_LinkedSlotRefs = m_LinkedSlots.Select(slot => slot.id.ToString()).ToList();
        }

       /* public virtual void OnAfterDeserialize()
        {
            base.OnBeforeSerialize();
        }*/

        private VFXExpression m_CachedExpression;
        private VFXExpression m_CurrentExpression;
        private VFXExpression m_DefaultExpression;
        private VFXExpression m_InExpression;
        private VFXExpression m_OutExpression;

        [NonSerialized]
        public IVFXSlotContainer m_Owner; // Don't set that directly! Only called by SlotContainer!

        [SerializeField]
        private VFXProperty m_Property;

        [SerializeField]
        private Direction m_Direction;

        private List<VFXSlot> m_LinkedSlots = new List<VFXSlot>();
        [SerializeField]
        private List<string> m_LinkedSlotRefs;
    }

    /*class VFXSlotFloat : VFXSlot<float>
    {

    }*/
}
