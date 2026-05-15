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
            lcs.enabled = true;
            lcs.lichPrefab = lichPrefab;
            lcs.mainCamera = mainCam;
            EditorUtility.SetDirty(lcs);
        }
        // Mago agora é VILÃO, não invocação. Desabilita MageCardSystem se existir.
        if (mcs != null)
        {
            mcs.enabled = false;
            EditorUtility.SetDirty(mcs);
        }

        // Remove MageCard antigo da UI se ainda existir
        GameObject oldMageCard = GameObject.Find("MageCard");
        if (oldMageCard != null)
        {
            Undo.DestroyObjectImmediate(oldMageCard);
            Debug.Log("[Setup] MageCard removido da UI (Mago virou vilão)");
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

        // Hero removido — limpa referência e zera click handler
        if (cs != null)
        {
            cs.cardImageHero = null;
            EditorUtility.SetDirty(cs);
        }

        // GolemCard
        GameObject golemCard = GameObject.Find("GolemCard");
        if (golemCard == null)
            golemCard = DuplicateCard(originalCard, "GolemCard", parent, basePos);
        if (cs != null)
        {
            var img = golemCard.GetComponent<Image>();
            if (img != null) { cs.cardImageGolem = img; EditorUtility.SetDirty(cs); }
            var btn = golemCard.GetComponent<Button>();
            if (btn != null) WireOnClick(btn, cs, "OnCardGolemClicked");
            var grt = golemCard.GetComponent<RectTransform>();
            if (grt != null) grt.anchoredPosition = basePos;
        }

        // LichCard
        GameObject lichCard = GameObject.Find("LichCard");
        if (lichCard == null)
            lichCard = DuplicateCard(originalCard, "LichCard", parent, basePos + new Vector2(0, -160));
        if (lcs != null)
        {
            var img = lichCard.GetComponent<Image>();
            if (img != null) { lcs.cardImage = img; EditorUtility.SetDirty(lcs); }
            var btn = lichCard.GetComponent<Button>();
            if (btn != null) WireOnClick(btn, lcs, "OnCardClicked");
            var lrt = lichCard.GetComponent<RectTransform>();
            if (lrt != null) lrt.anchoredPosition = basePos + new Vector2(0, -160);
        }

        // Destrói o card original do Hero (não usamos mais)
        if (originalCard != golemCard && originalCard != lichCard)
        {
            Undo.DestroyObjectImmediate(originalCard);
            Debug.Log("[Setup] HeroCard original removido");
        }

        // Mago não é mais carta — pulado.

        WireMapProgression();
        WirePlayerLives();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Setup] Concluído. Cena salva.");
    }

    static void WirePlayerLives()
    {
        GameObject grid = GameObject.Find("GridDeVida");
        if (grid == null)
        {
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
                if (t.name == "GridDeVida" && t.gameObject.scene.IsValid()) { grid = t.gameObject; break; }
        }
        if (grid == null) { Debug.LogWarning("[Setup] GridDeVida não encontrado"); return; }

        var pl = grid.GetComponent<PlayerLives>();
        if (pl == null) pl = grid.AddComponent<PlayerLives>();
        pl.gridDeVida = grid.transform;
        EditorUtility.SetDirty(pl);
        Debug.Log($"[Setup] PlayerLives configurado em GridDeVida ({grid.transform.childCount} vidas)");
    }

    static void WireMapProgression()
    {
        var magoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Manu/Mago.prefab");

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

            if (magoPrefab != null) sp.magoPrefab = magoPrefab;

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
