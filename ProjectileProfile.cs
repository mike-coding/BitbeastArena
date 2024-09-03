using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProjectileProfile 
{
    public static Dictionary<int, ProjectileProfile> Dex = new Dictionary<int, ProjectileProfile>();
    //"parentID" in old system
    public int ID;
    public int refID;
    public Sprite Sprite;
    public List<Sprite> Sprites = new List<Sprite>();
    public Color averageColor;
    public float spriteAlpha=1;
    public float velocityMagnitude=0;
    public float lifetimeDuration=0;
    public float rotationMagnitude = -1f;
    public float parentMagnitudeRatio = 0;
    public bool isPlayerProjectile=false;
    public bool inheritsParentVelocity = false;
    public bool pointsToDirection=true;
    public bool piercing = false;
    public bool dynamicDirection=false;
    public bool isOrbiter = false;
    public bool projectileCollisions = false;
    public bool usesFaux3DEffect=false;

    public void Init(int id,int referToID=0)
    {
        ID = id;
        refID = referToID;
        //load sprite
        if (refID!=0) Sprite = ProjectileProfile.Dex[refID].Sprite;
        else
        {
            string projectilePath = ID.ToString();
            if (isPlayerProjectile) projectilePath += "L";
            Sprite = Resources.Load<Sprite>($"Sprites/Projectiles/{projectilePath}");
            Sprites = Resources.LoadAll<Sprite>($"Sprites/Projectiles/{projectilePath}_anim").ToList();
        }
        //downscales to 4x4 and then calculates the average color
        averageColor = GameManager.CalculateAverageColor(Sprite,4,4);  
    }

    public static void LoadDex()
    {
        if (Dex.Count > 0) return;

        #region Projectile #1: Screech
        ProjectileProfile projectile1 = new ProjectileProfile();
        projectile1.Init(1);
        projectile1.lifetimeDuration = 1f;
        projectile1.velocityMagnitude = 2f;
        projectile1.spriteAlpha = 0.55f;
        Dex.Add(projectile1.ID, projectile1);
        #endregion

        #region Projectile #2: bubble
        ProjectileProfile projectile2 = new ProjectileProfile();
        projectile2.Init(2);
        projectile2.pointsToDirection = false;
        projectile2.lifetimeDuration = 1.25f;
        projectile2.velocityMagnitude = 2.5f;
        Dex.Add(projectile2.ID, projectile2);
        #endregion

        #region Projectile #3: slime shot
        ProjectileProfile projectile3 = new ProjectileProfile();
        projectile3.Init(3);
        projectile3.lifetimeDuration = 1.25f;
        projectile3.velocityMagnitude = 2.5f;
        Dex.Add(projectile3.ID, projectile3);
        #endregion

        //needs to inherit parent velocity
        #region Projectile #4: fungus spore
        ProjectileProfile projectile4 = new ProjectileProfile();
        projectile4.Init(4);
        projectile4.lifetimeDuration = 1f;
        projectile4.velocityMagnitude = 2f;
        projectile4.rotationMagnitude = 2.5f;
        //projectile4.inheritsParentVelocity = true;
        projectile4.pointsToDirection = false;
        Dex.Add(projectile4.ID, projectile4);
        #endregion

        #region Projectile #5: water gun
        ProjectileProfile projectile5 = new ProjectileProfile();
        projectile5.Init(5);
        projectile5.lifetimeDuration = 0.75f; //originally 0.25f
        projectile5.spriteAlpha = 0.75f;
        projectile5.velocityMagnitude = 4f; //originally 5.5f
        Dex.Add(projectile5.ID, projectile5);
        #endregion

        #region Projectile #6: psychic blast
        ProjectileProfile projectile6 = new ProjectileProfile();
        projectile6.Init(6);
        projectile6.lifetimeDuration = 0.25f;
        projectile6.velocityMagnitude = 4f;
        Dex.Add(projectile6.ID, projectile6);
        #endregion

        #region Projectile #10: stench!!!!
        ProjectileProfile projectile10 = new ProjectileProfile();
        projectile10.Init(10);
        projectile10.lifetimeDuration = 0.75f;
        projectile10.velocityMagnitude = 1f;
        projectile10.rotationMagnitude = 15f;
        projectile10.spriteAlpha = 0.9f;
        projectile10.inheritsParentVelocity = true;
        projectile10.piercing = true;
        Dex.Add(projectile10.ID, projectile10);
        #endregion

        #region Projectile #11: vine toss
        ProjectileProfile projectile11 = new ProjectileProfile();
        projectile11.Init(11);
        projectile11.lifetimeDuration = 1.5f;
        projectile11.velocityMagnitude = 2f;
        projectile11.rotationMagnitude = 35f;
        projectile11.usesFaux3DEffect=true;
        projectile11.pointsToDirection = false;
        Dex.Add(projectile11.ID, projectile11);
        #endregion

        #region Projectile #12: Fireball
        ProjectileProfile projectile12 = new ProjectileProfile();
        projectile12.Init(12);
        projectile12.lifetimeDuration = 1.75f;
        projectile12.velocityMagnitude = 3.5f;
        projectile12.spriteAlpha = 0.9f;
        projectile12.usesFaux3DEffect = true;
        projectile12.piercing = true;
        Dex.Add(projectile12.ID, projectile12);
        #endregion

        #region Projectile 13: Rock
        ProjectileProfile projectile13 = new ProjectileProfile();
        projectile13.Init(13);
        projectile13.lifetimeDuration = 0;
        projectile13.velocityMagnitude = 0f;
        projectile13.dynamicDirection = true;
        projectile13.pointsToDirection = false;
        projectile13.isOrbiter = true;
        projectile13.projectileCollisions = true;
        Dex.Add(projectile13.ID, projectile13);
        #endregion

        #region Projectile 14: Slimeball
        ProjectileProfile projectile14 = new ProjectileProfile();
        projectile14.Init(14);
        projectile14.lifetimeDuration = 0.4f;
        projectile14.velocityMagnitude = 3f;
        projectile14.spriteAlpha = 0.65f;
        projectile14.usesFaux3DEffect = true;
        projectile14.piercing = true;
        Dex.Add(projectile14.ID, projectile14);
        #endregion
    }
}

