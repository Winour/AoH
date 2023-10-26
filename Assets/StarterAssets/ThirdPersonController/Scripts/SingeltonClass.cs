using UnityEngine;

public class SingeltonClass<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if(_instance == null)
            {
                var loadedObject = Resources.Load("" + typeof(T).Name);
                if(loadedObject == null)
                {
                    loadedObject = new GameObject();
                }
                GameObject go = Instantiate(loadedObject) as GameObject;
                _instance = go.GetComponent<T>();
                if(_instance == null)
                {
                    _instance = (T)go.AddComponent(typeof(T));
                }
                _instance.name = typeof(T).Name;
            }
            return _instance;
        }
    }

    protected bool _destroyed;

    protected virtual void Awake()
    {
        if(_instance != null)
        {
            Destroy(this.gameObject);
            _destroyed = true;
        }
        else
        {
            _instance = this.gameObject.GetComponent<T>();
            DontDestroyOnLoad(this);
            _destroyed = false;
        }
    }
}
