// Project:      Create Atronach Mod for Daggerfall Unity
// Author:       DunnyOfPenwick
// Origin Date:  June 2021

using System.Collections;
using UnityEngine;
using DaggerfallWorkshop;

namespace CreateAtronachMod
{
    public class SummoningEgg
    {
        private readonly DaggerfallEnemy creature;
        private readonly Texture2D eggTexture;
        private readonly Color eggColor;
        private readonly GameObject outerEgg;
        private readonly GameObject innerEgg;
        private readonly AudioSource audioSource;
        private readonly AudioClip sound;

        public SummoningEgg(DaggerfallEnemy creature, Texture2D eggTexture, Color eggColor, AudioClip sound = null)
        {
            this.creature = creature;
            this.eggTexture = eggTexture;
            this.eggColor = eggColor;
            this.sound = sound;

            outerEgg = CreateOuterEgg();
            outerEgg.transform.parent = creature.transform.parent;

            audioSource = outerEgg.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f;
            audioSource.volume = 1.0f;
            
            innerEgg = CreateInnerEgg();
            innerEgg.transform.parent = outerEgg.transform;
            innerEgg.transform.localPosition = Vector3.zero;
            innerEgg.transform.localScale = new Vector3(0.90f, 0.90f, 0.90f);
        }

        public IEnumerator Hatch()
        {
            Vector2 size = creature.MobileUnit.GetSize();
            float creatureMidHeight = size.y * 0.5f;

            float yScale = 0.01f;
            float xzScale = size.x - 0.2f;
            outerEgg.transform.localScale = new Vector3(xzScale, yScale, xzScale);
            outerEgg.transform.position = creature.transform.position;
            outerEgg.transform.position -= outerEgg.transform.up * creatureMidHeight;
            outerEgg.SetActive(true);

            float scaleAdjustment = creatureMidHeight * 0.01f;

            if (sound != null)
            {
                audioSource.PlayOneShot(sound);
            }

            Material mat = innerEgg.GetComponent<Renderer>().material;

            while (yScale < creatureMidHeight)
            {
                yScale += scaleAdjustment;

                //grow the cylinder and adjust the position so that the base stays on the ground
                outerEgg.transform.localScale = new Vector3(xzScale, yScale, xzScale);
                outerEgg.transform.position += outerEgg.transform.up * scaleAdjustment;

                //brighten/dim color cyclically
                Color emissionColor = eggColor * Mathf.Abs(Mathf.Cos(Time.time * 8));
                mat.SetColor("_EmissionColor", emissionColor);

                outerEgg.transform.Rotate(0.0f, 16.0f, 0.0f);

                yield return new WaitForSeconds(.030f);
            }

            Object.Destroy(outerEgg);

            creature.gameObject.SetActive(true);
        }


        private GameObject CreateOuterEgg()
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Renderer renderer = cylinder.GetComponent<Renderer>();
            Material mat = renderer.material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            mat.mainTexture = eggTexture;

            //set some shader values for transparent rendering of outer warp-egg sphere texture
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            return cylinder;
        }

        private GameObject CreateInnerEgg()
        {
            //create an emissive inner sphere to make the warp-egg more visible and add some color
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            Renderer renderer = cylinder.GetComponent<Renderer>();
            Material mat = renderer.material;
            //renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

            mat.SetColor("_EmissionColor", eggColor);

            return cylinder;
        }

    }
}