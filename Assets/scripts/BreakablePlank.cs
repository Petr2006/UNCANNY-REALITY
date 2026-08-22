using UnityEngine;

public class BreakablePlank : MonoBehaviour
{
    [Header("Настройки прочности")]
    public int plankHealth = 1; // Сколько ударов нужно, чтобы сломать доску

    public void DamagePlank(Vector3 forceDirection)
    {
        plankHealth--;

        if (plankHealth <= 0)
        {
            Break(forceDirection);
        }
    }

    private void Break(Vector3 forceDirection)
    {
        Debug.Log("<color=red>[ДОСКА]</color> Доска " + gameObject.name + " сломана!");

        // 1. Убираем доску из слоя взаимодействия, чтобы в нее больше нельзя было ударить
        gameObject.layer = LayerMask.NameToLayer("Default");

        // 2. Добавляем физику Rigidbody (если его еще нет), чтобы доска упала
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // Настраиваем физику для красивого полета
        rb.isKinematic = false;
        rb.useGravity = true;

        // 3. Толкаем доску вперед по направлению удара
        rb.AddForce(forceDirection + Vector3.up * 100f); // Немного подкинем вверх
        rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 10f, ForceMode.Impulse); // Закрутим в полете

        // 4. Уничтожаем доску через 3 секунды после поломки, чтобы не засорять сцену
        Destroy(gameObject, 3f);
    }
}