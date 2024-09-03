using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum OrbiterType
{
    RockArmor
}

public class OrbitManager : MonoBehaviour
{
    private BeastController _parentBeast;
    private OrbiterType _type;
    private List<ProjectileController2D> _orbiters= new List<ProjectileController2D>();
    private Coroutine _orbitMovementRoutine;
    private int _projectileID;
    private int _maxOrbiters;
    private float _radius;
    private float _speed;
    private int _damage;
    private float _knockbackStrength;

    public void Init(BeastController parentBeast, OrbiterType type)
    {
        _parentBeast = parentBeast;
        _type = type;

        switch (_type)
        {
            case OrbiterType.RockArmor:
                _projectileID = 13;
                _maxOrbiters = 4;
                _radius = 0.635f;
                _speed = 5f;
                _damage = ((parentBeast.CurrentState.StatDict[Stat.MaxHP]/10)-3);
                _knockbackStrength = 6f;
                AddOrbiter(4);
                break;
        }
    }

    private IEnumerator MoveOrbiters()
    {
        while (true)
        {
            for (int i = 0; i < _orbiters.Count; i++)
            {
                if (_orbiters[i] != null)
                {
                    // Calculate angle for even spacing
                    float angle = (Time.time * _speed + i * Mathf.PI * 2 / _orbiters.Count) % (Mathf.PI * 2);
                    Vector2 orbitPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _radius;
                    Vector2 targetPosition = (Vector2)transform.position + orbitPosition;

                    Vector2 direction = (targetPosition - (Vector2)_orbiters[i].transform.position).normalized;
                    _orbiters[i].UpdateDamageDirection(direction);

                    // Smooth movement using Lerp
                    _orbiters[i].transform.position = Vector2.Lerp(_orbiters[i].transform.position, targetPosition, Time.deltaTime * _speed);

                    // Ensure the rotation is reset to match the manager's rotation
                    _orbiters[i].transform.localPosition = new Vector3(_orbiters[i].transform.localPosition.x, _orbiters[i].transform.localPosition.y, 0);
                }
                else
                {
                    ClearDeadOrbiters();
                    break;
                }
            }
            yield return null;
        }
    }

    public void AddOrbiter(int numberOfOrbiters = 1)
    {
        ClearDeadOrbiters();

        for (int count = 0; count < numberOfOrbiters; count++)
        {
            if (_orbiters.Count < _maxOrbiters)
            {
                GameObject proj = GameManager.GetProjectile();
                if (proj != null)
                {
                    proj.transform.parent = transform;
                    proj.transform.localPosition = Vector3.zero;
                    BillBoardSprite orbiterBillBoard = proj.AddComponent<BillBoardSprite>();
                    orbiterBillBoard.PointOnce = true;
                    ProjectileController2D newOrbiter = proj.GetComponent<ProjectileController2D>();
                    proj.SetActive(true);
                    DamageParticle damage = new DamageParticle();
                    damage.Init(_damage, _knockbackStrength, Vector2.zero, _parentBeast);
                    newOrbiter.Init(13, damage, !_parentBeast.IsEnemy, _parentBeast.Body);
                    _orbiters.Add(newOrbiter);
                }
            }
            else
            {
                break;
            }
        }

        if (_orbitMovementRoutine == null)
        {
            _orbitMovementRoutine = StartCoroutine(MoveOrbiters());
        }
    }

    public void ClearDeadOrbiters()
    {
        List<ProjectileController2D> deadOrbiters = new List<ProjectileController2D>();
        foreach (ProjectileController2D orbiter in _orbiters) if (!orbiter.gameObject.activeInHierarchy) deadOrbiters.Add(orbiter); // any null references?
        foreach (ProjectileController2D orbiter in deadOrbiters) _orbiters.Remove(orbiter); // delete em
    }
}
