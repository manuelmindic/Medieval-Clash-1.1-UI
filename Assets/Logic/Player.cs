using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : User
{
    private string ProfilePictureFileName { get; set; }
    public Player(string name, int rating, int healthPoints, int manaPoints, int money, string profilePictureFileName) : base(name, rating, healthPoints, manaPoints, money)
    {
        ProfilePictureFileName = profilePictureFileName;
    }
}
