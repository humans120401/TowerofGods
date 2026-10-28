using RT.Core.Combat;
using UnityEngine;

namespace RT.Boss
{
    /// <summary>
    /// 보스가 쏜 투사체. 방패 투척 · 파네스의 탄막이 이걸 쓴다.
    ///
    /// 맞든 안 맞든 수명이 끝나면 사라진다. 화면 밖에 남아 다음 패턴에 끼어들면
    /// "무엇에 맞았는지 모르겠다"가 되어 학습이 안 된다.
    ///
    /// 패링 가능 여부는 **쏜 패턴에서 그대로 실려 온다.**
    /// 투사체가 자기 플래그를 따로 들고 있으면 에셋과 어긋날 자리가 생긴다.
    /// </summary>
    [AddComponentMenu("Tower of Gods/보스/투사체")]
    public class BossProjectile : MonoBehaviour
    {
        [Tooltip("켜면 맞은 뒤 사라진다. 관통 탄막은 끈다")]
        [SerializeField] private bool destroyOnHit = true;

        [Tooltip("최대 수명(초). 이 시간이 지나면 사라진다")]
        [SerializeField][Min(0.1f)] private float lifetime = 5f;

        [Tooltip("플레이어를 따라가는 정도(도/초). 0이면 직선")]
        [SerializeField][Min(0f)] private float homingDegreesPerSecond;

        private HitInfo _info;
        private System.Action<float> _onParried;
        private LayerMask _targetLayers;
        private Vector2 _velocity;
        private Transform _homingTarget;
        private float _age;
        private bool _armed;

        /// <summary>
        /// 쏜 쪽이 불러서 내용을 채운다. Instantiate 직후에 반드시 한 번.
        /// </summary>
        public void Launch(
            in HitInfo info, Vector2 velocity, LayerMask targetLayers,
            Transform homingTarget = null, System.Action<float> onParried = null)
        {
            _info = info;
            _onParried = onParried;
            _velocity = velocity;
            _targetLayers = targetLayers;
            _homingTarget = homingTarget;
            _age = 0f;
            _armed = true;

            FacePath();
        }

        private void Update()
        {
            if (!_armed) return;

            _age += Time.deltaTime;

            if (_age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (homingDegreesPerSecond > 0f && _homingTarget != null) Steer();

            transform.position += (Vector3)(_velocity * Time.deltaTime);
        }

        /// <summary>유도 — 목표 쪽으로 조금씩 방향을 튼다. 완전 추적은 아니다.</summary>
        private void Steer()
        {
            Vector2 want = ((Vector2)_homingTarget.position - (Vector2)transform.position).normalized;
            if (want.sqrMagnitude < 0.0001f) return;

            float maxRadians = homingDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime;
            float speed = _velocity.magnitude;

            _velocity = Vector3.RotateTowards(_velocity.normalized, want, maxRadians, 0f)
                        * speed;

            FacePath();
        }

        private void FacePath()
        {
            if (_velocity.sqrMagnitude < 0.0001f) return;

            float angle = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_armed) return;

            // 레이어 마스크를 직접 확인한다. 콜라이더 설정만 믿으면 어긋나기 쉽다.
            if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

            var receiver = other.GetComponentInParent<IAttackReceiver>();
            if (receiver == null) return;

            float dir = other.transform.position.x < transform.position.x ? -1f : 1f;

            var info = new HitInfo(
                _info.PatternId, _info.HitIndex, _info.Damage, dir,
                _info.Knockback, _info.Parryable, _info.IgnoresDodge, _info.ParryStaggerTime);

            var result = receiver.ReceiveHit(in info);

            // 날아오는 것도 패링할 수 있어야 한다 — 설계서의 '방패 투척 후 대쉬 베기'가
            // 베기만 패링 가능한 것처럼, 가부는 패턴 에셋이 정한다.
            if (result == HitResult.Parried) _onParried?.Invoke(info.ParryStaggerTime);

            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
