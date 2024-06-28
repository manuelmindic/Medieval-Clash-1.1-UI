using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public class User
    {
        public string Name { get; set; }
        public int Rating { get; set; }

        public int HealthPoints { get; set; }
        public int ManaPoints { get; set; }
        public int Money { get; set; }
        public List<Card> UserDeck { get; set; }

        public User(string name, int rating, int healthPoints, int manaPoints, int money)
        {
            Name = name;
            Rating = rating;
            HealthPoints = healthPoints;
            ManaPoints = manaPoints;
            Money = money;
            UserDeck = new List<Card>();
        }

        public override string ToString()
        {
            return "Name: " + Name + "\nRating: " + Rating + "\nHealth Points: " + HealthPoints + "\nMana Points: " + ManaPoints + "\nMoney: " + Money;
        }

    }
    //ToDo: fragen wegen userinfo, was wollen wir da exactly?

