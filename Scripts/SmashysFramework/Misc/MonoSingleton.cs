using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    /// <summary>
    /// Base class for any object which there can only exist one of
    /// at a time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _inst;
        protected static T Inst
        {
            get
            {
                return _inst;//_inst ? _inst : _inst = FindObjectOfType<T>() ?? null;
            }
        }

        private void Register()
        {
            _inst = (T)this;
            Debug.Log($"{_inst.name} is now register");
        }

        protected virtual void Awake()
        {
            Debug.Log($"{name} is at the cash register");

            if (!_inst)
            {
                Debug.Log($"{name} paid for all his stuff");
                Register();
            }
            else if (_inst != this)
            {
                Debug.Log($"failed, i'm type {name}, inst's type is {_inst.name}");
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Returns if the singleton is of a specific type.
        /// (Useful if you're using a derived singleton)
        /// </summary>
        public static bool IsType(Type type)
        {
            return Inst && Inst.GetType() == type;
        }

        /// <summary>
        /// Base call re-instantiates this object (meaning all of its variables as well)
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_inst == this)
            {
                _inst = null;
                //_inst = (T)Instantiate(this);
            }
        }

        /// <summary>
        /// Treat this like a regular Awake() call. This runs after the
        /// singleton is successfully registered. You can remove the base
        /// call from your code, it is empty anyway.
        /// </summary>
        //protected virtual void SubAwake() { }
    }

}
