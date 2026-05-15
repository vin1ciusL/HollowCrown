using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;

public static class QuickSetupCards
{
    [MenuItem("Tools/Auto Setup Cards")]
    public static void Setup()
    {
        var lichPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Manu/Lich.prefab");
        var magoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Manu/Mago.prefab");
        var golemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ken/Golem.prefab");
        var heroPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Ken/Hero.prefab");

        Debug.Log($"[Setup] Lich={(lichPrefab!=null)} Mago={(magoPrefab!=null)} Golem={(golemPrefab!=null)} Hero={(heroPrefab!=null)}");

        Camera mainCam = Camera.main;
        if (mainCam == null) mainCam = Object.FindFirstObjectByType<Camera>();

        var cardSystemGO = GameObject.Find("CardSystem");
        if (cardSystemGO == null)
        {
            Debug.LogError("[Setup] CardSystem GameObject não encontrado");
            return;
        }

        var cs = cardSystemGO.GetComponent<CardSystem>();
        var lcs = cardSystemGO.GetComponent<LichCardSystem>();
        var mcs = cardSystemGO.GetComponent<MageCardSystem>();

        if (cs != null)
        {
            cs.heroPrefab = heroPrefab;
            cs.golemPrefab = golemPrefab;
            cs.mainCamera = mainCam;
            EditorUtility.SetDirty(cs);
        }
        if (lcs != null)
        {
            lcs.lichPrefab = lichPrefab;
            lcs.mainCamera = mainCam;
            EditorUtility.SetDirty(lcs);
        }
        if (mcs != null)
        {
            mcs.magePrefab = magoPrefab;
            mcs.mainCamera = mainCam;
            EditorUtility.SetDirty(mcs);
        }

        // Encontrar a Card existente
        GameObject originalCard = GameObject.Find("Card");
        if (originalCard == null)
        {
            Debug.LogError("[Setup] Card original não encontrado no Canvas");
            EditorSceneManager.SaveOpenScenes();
            return;
        }

        var origRT = originalCard.GetComponent<RectTransform>();
        Vector2 basePos = origRT != null ? origRT.anchoredPosition : Vector2.zero;
        Transform parent = originalCard.transform.parent;

        // Wire HeroCard original para CardSystem.OnCardHeroClicked
        if (cs != null)
        {
            var origImg = originalCard.GetComponent<Image>();
            if (origImg != null) { cs.cardImageHero = origImg; EditorUtility.SetDirty(cs); }
            var origBtn = originalCard.GetComponent<Button>();
            if (origBtn != null) WireOnClick(origBtn, cs, "OnCardHeroClicked");
        }

        // Duplicar pra Golem
        if (cs != null && cs.cardImageGolem == null)
        {
            var golemCard = DuplicateCard(originalCard, "GolemCard", parent, basePos + new Vector2(110, 0));
            var img = golemCard.GetComponent<Image>();
            if (img != null) { cs.cardImageGolem = img; EditorUtility.SetDirty(cs); }
            var btn = golemCard.GetComponent<Button>();
            if (btn != null) WireOnClick(btn, cs, "OnCardGolemClicked");
        }

        // Duplicar pra Mago
        if (mcs != null && mcs.cardImage == null)
        {
            var mageCard = DuplicateCard(originalCard, "MageCard", parent, basePos + new Vector2(220, 0));
            var img = mageCard.GetComponent<Image>();
            if (img != null) { mcs.cardImage = img; EditorUtility.SetDirty(mcs); }
            var btn = mageCard.GetComponent<Button>();
            if (btn != null) WireOnClick(btn, mcs, "OnCardClicked");
        }

        // Duplicar pra Lich
        if (lcs != null && lcs.cardImage == null)
        {
            var lichCard = DuplicateCard(originalCard, "LichCard", parent, basePos + new Vector2(330, 0));
            var img = lichCard.GetComponent<Image>();
            if (img != null) { lcs.cardImage = img; EditorUtility.SetDirty(lcs); }
            var btn = lichCard.GetComponent<Button>();
            if (btn != null) WireOnClick(btn, lcs, "OnCardClicked");
        }

        WireMapProgression();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Concluído. Cena salva.");
    }

    static void WireMapProgression()
    {
        // Ordem: começa pelo mapa ativo. Padrão: Externo → Royal → Dungeon.
        string[] order = { "Mapa_Externo", "Mapa_Dungeon", "Mapa_Royal" };
        GameObject[] maps = new GameObject[order.Length];
        for (int i = 0; i < order.Length; i++)
        {
            maps[i] = GameObject.Find(order[i]);
            if (maps[i] == null)
            {
                foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                    if (t.name == order[i] && t.gameObject.scene.IsValid()) { maps[i] = t.gameObject; break; }
            }
        }

        // Pega bounds de referência do Externo (que está com bounds corretos)
        Vector2 refMin = new Vector2(-27, -13.5f);
        Vector2 refMax = new Vector2(-4.5f, 0.5f);
        if (maps[0] != null)
        {
            var refSp = maps[0].GetComponentInChildren<VillainSpawner>(true);
            if (refSp != null && refSp.mapMin != refSp.mapMax &&
                Mathf.Abs(refSp.mapMin.x) < 100 && Mathf.Abs(refSp.mapMax.x) < 100)
            {
                refMin = refSp.mapMin;
                refMax = refSp.mapMax;
            }
        }

        for (int i = 0; i < order.Length; i++)
        {
            if (maps[i] == null) { Debug.LogWarning($"[Setup] {order[i]} não encontrado"); continue; }
            var sp = maps[i].GetComponentInChildren<VillainSpawner>(true);
            if (sp == null) { Debug.LogWarning($"[Setup] VillainSpawner não achado em {order[i]}"); continue; }

            // Corrige bounds inválidos (coordenadas antigas longe da posição atual do mapa)
            if (Mathf.Abs(sp.mapMin.x) > 100 || Mathf.Abs(sp.mapMax.x) > 100 || sp.mapMin == sp.mapMax)
            {
                sp.mapMin = refMin;
                sp.mapMax = refMax;
                Debug.Log($"[Setup] {order[i]} bounds corrigidos para min={refMin} max={refMax}");
            }

            sp.mapaAtual = maps[i];
            GameObject prox = (i + 1 < maps.Length) ? maps[i + 1] : null;
            sp.proximoMapa = prox;
            if (prox != null)
            {
                var nextSp = prox.GetComponentInChildren<VillainSpawner>(true);
                Vector2 camPos;
                if (nextSp != null && nextSp.mapMin != nextSp.mapMax)
                    camPos = (nextSp.mapMin + nextSp.mapMax) * 0.5f;
                else
                    camPos = new Vector2(prox.transform.position.x, prox.transform.position.y);
                sp.posicaoCameraProximoMapa = camPos;
            }

            EditorUtility.SetDirty(sp);
            Debug.Log($"[Setup] {order[i]} → proximoMapa={(prox?prox.name:"null")} bounds=({sp.mapMin},{sp.mapMax})");
        }
    }

    static GameObject DuplicateCard(GameObject src, string newName, Transform parent, Vector2 anchoredPos)
    {
        var copy = Object.Instantiate(src, parent);
        copy.name = newName;
        var rt = copy.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = anchoredPos;
        Undo.RegisterCreatedObjectUndo(copy, "Create " + newName);
        return copy;
    }

    static void WireOnClick(Button btn, MonoBehaviour target, string methodName)
    {
        // Limpa persistent listeners do tipo certo (pra não duplicar)
        for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (btn.onClick.GetPersistentTarget(i) == target &&
                btn.onClick.GetPersistentMethodName(i) == methodName)
                return;
        }
        UnityAction action = System.Delegate.CreateDelegate(typeof(UnityAction), target, methodName) as UnityAction;
        UnityEventTools.AddPersistentListener(btn.onClick, action);
        EditorUtility.SetDirty(btn);
    }
}
