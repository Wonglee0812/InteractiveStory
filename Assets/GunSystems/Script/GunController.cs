using UnityEngine;
using UnityEngine.Pool;
using DG.Tweening;

[DisallowMultipleComponent]
public class GunController : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField]private WeaponInventory inventory;
    [SerializeField] private GunType Gun;
    [SerializeField] private Transform GunParent;


    private GunData activeGun;
    private void Start()
    {

        GunData gun = inventory.availableGuns.Find(gun => gun.Type == Gun);
        activeGun = inventory.CurrentWeapon;

        if (activeGun != null)
        {
            gun.Spawn(GunParent, this);
        }

    }

    public void Shoot()
    {
        if (activeGun != null)
        {
            activeGun.Shoot();
        }    
    }

    //AI
    public void ShootAtAI(Vector3 targetPosition)
    {
        if (activeGun != null)
        {
            activeGun.ShootAtAI(GunParent.position, targetPosition, this);
        }
    }
}