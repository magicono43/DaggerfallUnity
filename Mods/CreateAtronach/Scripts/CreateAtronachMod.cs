// Project:   Create Atronach Mod for Daggerfall Unity
// Author:    DunnyOfPenwick
// Origin Date:  June 2021

using System;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Utility;

namespace CreateAtronachMod
{
    public class CreateAtronachMod : MonoBehaviour
    {
        private static Mod mod;
        private CreateAtronach templateEffect;
        public static CreateAtronachMod Instance;
        public Texture2D SummoningEggTexture;
        public AudioClip WarpIn;


        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;

            var go = new GameObject(mod.Title);
            go.AddComponent<CreateAtronachMod>();
        }


        void Awake()
        {
            Instance = this;
            InitMod();
            mod.IsReady = true;
        }


        public void InitMod()
        {
            Debug.Log("Begin mod init: CreateAtronach");

            LoadTextures();

            if (!ModManager.Instance.TryGetAsset("WarpIn", false, out WarpIn))
            {
                throw new Exception("Missing WarpIn sound asset");
            }

            templateEffect = new CreateAtronach();
            GameManager.Instance.EntityEffectBroker.RegisterEffectTemplate(templateEffect);

            SaveLoadManager.OnLoad += SaveLoadManager_OnLoad;

            Debug.Log("Finished mod init: CreateAtronach");
        }


        private void LoadTextures()
        {
            //using native game texture for summoning egg
            GetTextureSettings settings = new GetTextureSettings();
            settings.archive = 157;
            settings.record = 1;
            settings.frame = 0;
            settings.stayReadable = true;
            GetTextureResults results = DaggerfallUnity.Instance.MaterialReader.TextureReader.GetTexture2D(settings);

            SummoningEggTexture = results.albedoMap;

            //modify alpha transparency of the texture
            Color32[] cols = SummoningEggTexture.GetPixels32();
            for (var i = 0; i < cols.Length; ++i)
            {
                cols[i].a = (byte)(255 - cols[i].r);
            }

            SummoningEggTexture.SetPixels32(cols);
            SummoningEggTexture.Apply(false);
        }


        //Attempt to preload decoy textures to reduce hiccups during play
        private void SaveLoadManager_OnLoad(SaveData_v1 saveData)
        {
            Transform parent = GameObjectHelper.GetBestParent();
            GameObject go = GameObjectHelper.InstantiatePrefab(DaggerfallUnity.Instance.Option_EnemyPrefab.gameObject, "temp", parent, Vector3.zero);
            SetupDemoEnemy setupEnemy = go.GetComponent<SetupDemoEnemy>();

            //Cache atronach textures
            setupEnemy.ApplyEnemySettings(MobileTypes.FireAtronach, MobileReactions.Hostile, MobileGender.Male, 0, true);
            setupEnemy.ApplyEnemySettings(MobileTypes.FleshAtronach, MobileReactions.Hostile, MobileGender.Male, 0, true);
            setupEnemy.ApplyEnemySettings(MobileTypes.IceAtronach, MobileReactions.Hostile, MobileGender.Male, 0, true);
            setupEnemy.ApplyEnemySettings(MobileTypes.IronAtronach, MobileReactions.Hostile, MobileGender.Male, 0, true);

            Destroy(go);

            //should only have to call once
            SaveLoadManager.OnLoad -= SaveLoadManager_OnLoad;
        }

    }
}