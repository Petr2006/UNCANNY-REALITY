using UnityEngine;

public class CrowbarController : MonoBehaviour
{
    [Header("Настройки удара")]
    public float attackDistance = 3f; // Дистанция удара
    [Tooltip("Сила, с которой будут отлетать сломанные доски")]
    public float breakForce = 300f;

    public void TryAttack()
    {
        Debug.Log("<color=yellow>[ЛОМ]</color> Сделан замах ломом!");

        // Пускаем луч из центра экрана
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        // Фильтруем попадания только по нашему слою Interactable
        int layerMask = 1 << LayerMask.NameToLayer("Interactable");

        if (Physics.Raycast(ray, out hit, attackDistance, layerMask))
        {
            // Пытаемся найти на объекте скрипт ломающейся доски
            BreakablePlank plank = hit.transform.GetComponent<BreakablePlank>();
            if (plank != null)
            {
                // Наносим урон доске и передаем направление удара для красивого отлета физики
                plank.DamagePlank(ray.direction * breakForce);
            }
        }
    }
}