using UnityEngine;

public class Comportamiento : MonoBehaviour
{
    public int numero;
    [SerializeField] private int queso;
    int refresco;

    public void Calentar()
    {

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int pan;
        pan = 2;
        numero = 0;
        Debug.Log(pan);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
