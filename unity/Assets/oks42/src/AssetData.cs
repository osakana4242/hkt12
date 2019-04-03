using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
	using UnityEditor;
#endif

namespace Osk42 {
	[CreateAssetMenu]
	[System.Serializable]
	public sealed class AssetData : ScriptableObject {

		[System.Serializable]
		public sealed class Config {
			public float cameraSpeed = 5f;
			public float bulletSpeed = 10f;
			public Vector3 fireOffset = new Vector3(0f, 0.75f, 0f);
		}

		public Config config;

		[System.Serializable]
		public sealed class Spawn {
			public float time;
			public Vector2 position;
			public float speed;
		}

		[System.Serializable]
		public sealed class Wave {
			public Spawn[] spawnArr;
		}

		public Wave[] waveArr;


		Dictionary<string, Object> dict_;
		Dictionary<string, Object> Dict {
			get {
				if ( dict_ != null ) return dict_;
				dict_ = new Dictionary<string, Object>(assets.Length);
				foreach (var item in assets) {
					dict_[item.name] = item;
				}
				return dict_;
			}
		}

		public Object[] assets;

		public T getAsset<T>(string name) where T : Object {
			var t = Dict[name] as T;
			return t;
		}
	}
}
