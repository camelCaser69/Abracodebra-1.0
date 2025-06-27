using UnityEngine;

[CreateAssetMenu(fileName = "NewAnimalThoughtLibrary", menuName = "Ecosystem/Animal Thought Library")]
public class AnimalThoughtLibrary : ScriptableObject
{
    [Header("Thought Messages")]
    public string[] hungryThoughts = new string[] 
    { 
        "I'm hungry!", 
        "Need food...", 
        "Where's the food?" 
    };
    
    public string[] eatingThoughts = new string[] 
    { 
        "Yum!", 
        "Delicious!", 
        "Nom nom nom" 
    };
    
    public string[] healthLowThoughts = new string[] 
    { 
        "I don't feel good...", 
        "Help me!", 
        "Ouch!" 
    };
    
    public string[] fleeingThoughts = new string[] 
    { 
        "Run away!", 
        "Scary!", 
        "Help!" 
    };
    
    public string[] poopingThoughts = new string[] 
    { 
        "Nature calls!", 
        "Gotta go!", 
        "..." 
    };
}