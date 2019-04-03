using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Osk42 {
	public sealed class MainPart : MonoBehaviour {
		public AppCore appCore;
		public StateMachine<MainPart> sm;
		// Start is called before the first frame update
		public CharaBase player;
		public Vector3 shotDir;
		public Transform cameraAnchor;
		public List<CharaBase> charaList;
		public int autoIncrement;
		public GameObject prisonGroup;
		public GameObject prison;
		public int waveIndex;
		public int waveLoopCount;
		public int spawnIndex;
		public float spawnTime;
		public int score;
		public UnityEngine.UI.Text uiText;
		public bool hasPlayerDamage;

		public int createGameInstanceId() {
			return ++autoIncrement;
		}

		void Awake() {
			sm = new StateMachine<MainPart>();
		}

		void Start() {
			sm.SwitchState(StateFunc.init_g);
		}

		// Update is called once per frame
		void Update() {
			refleshCharaList(charaList);
			sm.Update(this);
		}

		public static void TracePlayer(MainPart self, Transform cameraTr, Player player, List<CharaBase> friendList) {
			var bounds = GetBounds(self, player, friendList);
			var pos = cameraTr.position;
			var tpos = bounds.center + player.transform.forward * 2f;
			var npos = Vector3.Lerp(pos, tpos, self.appCore.assetData.config.cameraSpeed * Time.deltaTime);
			pos = npos;
			cameraTr.position = pos;
		}

		public static Bounds GetBounds(MainPart self, Player player, List<CharaBase> friendList) {
			var ppos = player.transform.position;
			var min = ppos;
			var max = ppos;
			foreach (var friend in friendList) {
				var fpos = friend.transform.position;
				min = Vector3.Min(min, fpos);
				max = Vector3.Max(max, fpos);
			}
			var bounds = new Bounds();
			bounds.SetMinMax(min, max);
			return bounds;
		}

		public static void TraceNode(Player player, List<CharaBase> friendList) {
			for (var i = 0; i < friendList.Count; i++) {
				var firend = friendList[i];
				var node = Player.findNode(player, firend.gameInstanceId);
				Slave.TraceNode(firend.slave(), node);
			}
		}

		public static void UpdateRing(List<PlayerNode> nodeList, float tangle, float angleSpeed) {
			for (var ni = 0; ni < nodeList.Count; ni++) {
				var node = nodeList[ni];
				var tangle2 = ni == 0 ? tangle / 2 : tangle;
				var next = Mathf.MoveTowardsAngle(node.localAngle, tangle2, angleSpeed * Time.deltaTime);
				node.targetLocalAngle = tangle2;
				node.localAngle = next;
				if (node.localAngle != tangle2) break;
			}
		}

		public static bool IsCloseRing(Player player) {
			var cnt = player.nodeListL.Count + player.nodeListR.Count - 1;
			if (cnt < 3) return false;

			var list = player.nodeListL;
			var index = list.FindIndex(_node => !_node.IsClose());
			if (0 <= index && index < list.Count - 1) return false;
			list = player.nodeListR;
			index = list.FindIndex(_node => !_node.IsClose());
			if (0 <= index && index < list.Count - 1) return false;
			return true;
		}

		public static TempolaryListPool<CharaBase>.Container findChara(List<CharaBase> list, System.Predicate<CharaBase> isMatch) {
			var lc = TempolaryListPool<CharaBase>.instance.alloc();
			for (var i = 0; i < list.Count; i++) {
				var item = list[i];
				if (!isMatch(item)) continue;
				lc.list.Add(item);
			}
			return lc;
		}

		public static TempolaryListPool<T>.Container findChara<T>(List<CharaBase> list, System.Predicate<CharaBase> isMatch) {
			var lc = TempolaryListPool<T>.instance.alloc();
			for (var i = 0; i < list.Count; i++) {
				var item = list[i];
				if (!isMatch(item)) continue;
				var comp = item.GetComponent<T>();
				lc.list.Add(comp);
			}
			return lc;
		}

		public static bool isHit(CharaBase c1, CharaBase c2, float length) {
			var v = c1.transform.position - c2.transform.position;
			var sqrLength = length * length;
			if (sqrLength < v.sqrMagnitude) return false;
			return true;
		}

		/** null を除去. */
		public static void refleshCharaList(List<CharaBase> list) {
			for (var i = list.Count - 1; 0 <= i; i--) {
				var item = list[i];
				if (item != null) continue;
				list.RemoveAt(i);
			}
		}

		public static bool waveUpdate(MainPart self) {
			// spawn
			bool hasEnemy;
			using (var lc = findChara(self.charaList, _chara => _chara.type == CharaType.Enemy)) {
				hasEnemy = 0 < lc.list.Count;
			}
			var waveArr = self.appCore.assetData.waveArr;
			if (self.waveIndex < waveArr.Length) {
				var spawnArr = waveArr[self.waveIndex].spawnArr;
				if (self.spawnIndex < spawnArr.Length) {
					var spawn = spawnArr[self.spawnIndex];
					if (self.spawnTime < spawn.time) {
						self.spawnTime += Time.deltaTime;
					} else {
						var enemyPrefab = self.appCore.assetData.getAsset<GameObject>("enemy");
						var enemyPos = self.prison.transform.position + new Vector3(spawn.position.x, spawn.position.y, 0f) * 10f;
						var enemyRot = Quaternion.LookRotation(self.player.transform.position - enemyPos);
						var enemy = GameObject.Instantiate(enemyPrefab, enemyPos, enemyRot, self.transform).GetComponent<CharaBase>();
						enemy.gameInstanceId = self.createGameInstanceId();
						enemy.OnCollisionEnterAsObservable().
							TakeUntilDestroy(enemy.gameObject).
							Subscribe(_coll => {
								if (enemy.hp <= 0) return;
								if (_coll.gameObject.layer != LayerMask.NameToLayer("player")) return;
								self.hasPlayerDamage = true;
							});


						var speed = spawn.speed * (self.waveLoopCount + 1);
						enemy.enemy().moveSpeed *= speed;
						enemy.enemy().angleSpeed *= speed;
						self.charaList.Add(enemy);
						self.spawnIndex += 1;
					}
				} else {
					if (!hasEnemy) {
						self.waveIndex += 1;
						self.spawnIndex = 0;
						self.spawnTime = 0f;
					}
				}
			} else {
				if (!hasEnemy) {
					self.waveIndex = 0;
					self.spawnIndex = 0;
					self.spawnTime = 0f;
					self.waveLoopCount += 1;
					return true;
				}
			}
			return false;
		}
		public static class StateFunc {
			public static readonly StateMachine<MainPart>.StateFunc reset_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Update: {
							self.charaList.ForEach(_item => Object.Destroy(_item.gameObject));
							self.charaList.Clear();
							self.player = null;
							Random.InitState(65536);
							self.autoIncrement = 0;
							self.waveIndex = 0;
							self.spawnIndex = 0;
							self.spawnTime = 0f;
							self.waveLoopCount = 0;
							self.score = 0;
							self.hasPlayerDamage = false;
							self.uiText.text = "";
							if (self.prisonGroup) {
								GameObject.Destroy(self.prisonGroup);
							}
							self.prison = null;
							self.prisonGroup = null;
							return StateMachine<MainPart>.Result.Change(init_g);
						}
				}
				return StateMachine<MainPart>.Result.Default;
			};

			public static readonly StateMachine<MainPart>.StateFunc init_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Init: {
							var prisonGroupPrefab = self.appCore.assetData.getAsset<GameObject>("prison_group");
							var prisonGroup = GameObject.Instantiate(prisonGroupPrefab, Vector3.zero, Quaternion.identity, self.transform);
							self.prisonGroup = prisonGroup;
							self.prison = prisonGroup.transform.Find("prison").gameObject;

							var playerPrefab = self.appCore.assetData.getAsset<GameObject>("player");

							// ダメージを食らったあとの振り向きで次の出現を予測できる？

							var player = GameObject.Instantiate(playerPrefab, self.prison.transform.position + new Vector3(0f, -1f, 0f), Quaternion.identity, self.prison.transform).GetComponent<CharaBase>();
							player.gameInstanceId = self.createGameInstanceId();
							self.charaList.Add(player);
							self.player = player;
							self.uiText.text = "Z キーで開始";
							break;
						}
					case StateMachine.Operation.Update: {
							{
								if (Input.GetKeyDown(KeyCode.Z)) {
									return StateMachine<MainPart>.Result.Change(main_g);
								}
							}
							break;
						}
				}
				return StateMachine<MainPart>.Result.Default;
			};

			public static readonly StateMachine<MainPart>.StateFunc main_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Init: {
							self.uiText.text = "";
							break;
						}
					case StateMachine.Operation.Update: {
							{
								{
									var shotDir = Vector3.zero;
									if (Input.GetKey(KeyCode.LeftArrow)) {
										shotDir.x = -1f;
									} else if (Input.GetKey(KeyCode.RightArrow)) {
										shotDir.x = 1f;
									}
									if (Input.GetKey(KeyCode.DownArrow)) {
										shotDir.y = -1f;
									} else if (Input.GetKey(KeyCode.UpArrow)) {
										shotDir.y = 1f;
									}

									if (self.shotDir == Vector3.zero) {
										self.shotDir = Vector3.right;
									}
									
									if (shotDir != Vector3.zero) {
										self.shotDir = shotDir;
										var forward = self.player.transform.localRotation * Vector3.forward;
										forward.x = shotDir.x;
										forward.z = shotDir.y;
										forward.y = 0f;
										self.player.transform.localRotation = Quaternion.LookRotation(forward);
									}

									if (Input.GetKeyDown(KeyCode.Z)) {
										if (shotDir == Vector3.zero) {
											shotDir = self.shotDir;
										}
										var bulletPrefab = self.appCore.assetData.getAsset<GameObject>("bullet");
										var pos = self.player.transform.position + self.appCore.assetData.config.fireOffset;
										var bullet = GameObject.Instantiate(bulletPrefab, pos, Quaternion.LookRotation(shotDir), self.transform).GetComponent<CharaBase>();
										bullet.gameInstanceId = self.createGameInstanceId();
										bullet.OnCollisionEnterAsObservable().
											TakeUntilDestroy(bullet.gameObject).
											Subscribe(_coll => {
												var other = _coll.gameObject.GetComponentInParent<CharaBase>();
												if (other == null) return;
												if (other.type != CharaType.Enemy) return;
												if (other.hp <= 0) return;
												other.hp -= 1;
												self.score += 1;
												GameObject.Destroy(bullet.gameObject);
											});
										self.charaList.Add(bullet);
									}
								}
								{
									if (Input.GetKeyDown(KeyCode.R)) {
										return StateMachine<MainPart>.Result.Change(reset_g);
									}
								}

								waveUpdate(self);

								using (var lc = findChara(self.charaList, _chara => _chara.type == CharaType.PlayerBullet)) {
									foreach (var chara in lc.list) {
										var pos = chara.transform.position;
										pos += chara.transform.forward * Time.deltaTime * self.appCore.assetData.config.bulletSpeed;
										chara.transform.position = pos;
										if (2f <= chara.time) {
											GameObject.Destroy(chara.gameObject);
										}
									}
								}
								using (var lc = findChara(self.charaList, _chara => _chara.type == CharaType.Enemy)) {
									foreach (var chara in lc.list) {
										Enemy.Update(chara.enemy(), self);
									}
								}
								self.charaList.ForEach(_item => CharaBase.updateAnim(_item));
							}
							if (self.hasPlayerDamage) {
								return StateMachine<MainPart>.Result.Change(dead_g);
							}
							self.uiText.text = string.Format("スコア {0}", self.score);
							break;
						}
				}
				return StateMachine<MainPart>.Result.Default;
			};
			public static readonly StateMachine<MainPart>.StateFunc dead_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Init: {
							break;
						}
					case StateMachine.Operation.Update: {
							{
								if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Z)) {
									return StateMachine<MainPart>.Result.Change(reset_g);
								}
							}

							waveUpdate(self);

							using (var lc = findChara(self.charaList, _chara => _chara.type == CharaType.PlayerBullet)) {
								foreach (var chara in lc.list) {
									var pos = chara.transform.position;
									pos += chara.transform.forward * Time.deltaTime * self.appCore.assetData.config.bulletSpeed;
									chara.transform.position = pos;
									if (2f <= chara.time) {
										GameObject.Destroy(chara.gameObject);
									}
								}
							}
							using (var lc = findChara(self.charaList, _chara => _chara.type == CharaType.Enemy)) {
								foreach (var chara in lc.list) {
									Enemy.Update(chara.enemy(), self);
								}
							}
							self.charaList.ForEach(_item => CharaBase.updateAnim(_item));
							self.uiText.text = string.Format("スコア {0}\n", self.score) +
								string.Format("ゲームオーバー\n") +
								string.Format("Zキーで次へ");
							break;
						}
				}
				return StateMachine<MainPart>.Result.Default;
			};
		}
	}
}
