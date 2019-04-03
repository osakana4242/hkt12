using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Osk42 {
	public class Enemy : MonoBehaviour {
		public CharaBase charaBase;
		public float moveSpeed = 20f;
		public float angleSpeed = 360f;
		public Vector3 targetPosition1;
		public Vector3 targetPosition2;
		public Vector3 firstForward;
		public Vector3 firstPosition;
		public StateMachine<Enemy> sm;

		// prison を目的地にする
		// 到達したらそのまままっすぐ進んで消える
		// 被弾したら次の出現方向を示してから帰る

		void Awake() {
			sm = new StateMachine<Enemy>();
		}

		// Start is called before the first frame update
		void Start() {
			sm.SwitchState(StateFunc.main_g);
		}

		public static void Update(Enemy self, MainPart part) {
			var playerTr = part.player.transform;
			self.targetPosition1 = playerTr.position + playerTr.up * 2f + self.firstForward * 10f;
			self.sm.Update(self);
		}

		void OnDrawGizmos() {
			//			Gizmos.DrawWireSphere(targetPosition, 0.5f);
		}

		public static class StateFunc {
			public static readonly StateMachine<Enemy>.StateFunc main_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Init: {
							self.firstForward = self.transform.forward;
							self.firstPosition = self.transform.position;
							self.targetPosition2 = self.firstPosition;
							break;
						}
					case StateMachine.Operation.Update: {
							var position = self.transform.position;
							var targetPosition = self.targetPosition1;
							var sqrDistance = (targetPosition - position).sqrMagnitude;
							var rot = self.transform.rotation;
							var trot = Quaternion.LookRotation(targetPosition - position);
							var nrot = Quaternion.RotateTowards(rot, trot, self.angleSpeed * Time.deltaTime);
							var delta = nrot * Vector3.forward * self.moveSpeed * Time.deltaTime;
							position += delta;
							var rb = self.transform.GetComponent<Rigidbody>();
							rb.position = position;
							rb.rotation = nrot;
							// self.transform.position = position;
							// self.transform.rotation = nrot;
							self.charaBase.hasWalk = true;
							if (self.charaBase.hp <= 0) {
								return StateMachine<Enemy>.Result.Change(StateFunc.damage_g);
							}
							if (sqrDistance < 0.5f * 0.5f) {
								return StateMachine<Enemy>.Result.Change(StateFunc.dead_g);
							}
							break;
						}
				}
				return StateMachine<Enemy>.Result.Default;
			};

			public static readonly StateMachine<Enemy>.StateFunc damage_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Init: {

							break;
						}
					case StateMachine.Operation.Update: {
							var position = self.transform.position;
							var targetPosition = self.targetPosition2;
							var sqrDistance = (targetPosition - position).sqrMagnitude;
							var rot = self.transform.rotation;
							var trot = Quaternion.LookRotation(targetPosition - position);
							var nrot = Quaternion.RotateTowards(rot, trot, self.angleSpeed * Time.deltaTime);
							var delta = nrot * Vector3.forward * self.moveSpeed * Time.deltaTime;
							position += delta;
							self.transform.position = position;
							self.transform.rotation = nrot;
							self.charaBase.hasWalk = true;
							if (sqrDistance < 0.5f * 0.5f) {
								return StateMachine<Enemy>.Result.Change(StateFunc.dead_g);
							}
							break;
						}
				}
				return StateMachine<Enemy>.Result.Default;
			};

			public static readonly StateMachine<Enemy>.StateFunc dead_g = (self, ope) => {
				switch (ope) {
					case StateMachine.Operation.Init: {
							GameObject.Destroy(self.gameObject);
							break;
						}
					case StateMachine.Operation.Update: {
							break;
						}
				}
				return StateMachine<Enemy>.Result.Default;
			};
		}
	}
}
