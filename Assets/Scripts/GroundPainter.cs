using UnityEngine;

public class GroundPainter : MonoBehaviour
{
    [SerializeField] private GameObject groundTilemap;
    [SerializeField] private GameObject groundPalette;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 10 x 10 grid
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                // Instantiate a ground tile
                GameObject groundTile = Instantiate(groundPalette.transform.GetChild(Random.Range(0, groundPalette.transform.childCount)).gameObject, new Vector3(x, y, 0), Quaternion.identity);
                groundTile.transform.SetParent(groundTilemap.transform);
            }
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
