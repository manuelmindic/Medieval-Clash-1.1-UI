using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    private string _imageFileName;
    private string _name;
    private int _price;
    private TypeOfCard _typeOfCard;
    private int _damage;
    private int _defense;
    private int _manaCost;

    public Card(string imageFileName, string name, int price, TypeOfCard typeOfCard, int damage, int defense, int manaCost)
    {
        _imageFileName = imageFileName;
        _name = name;
        _price = price;
        _typeOfCard = typeOfCard;
        _damage = damage;
        _defense = defense;
        _manaCost = manaCost;
    }

    public string ImageFileName { get => _imageFileName; set => _imageFileName = value; }
    public string Name { get => _name; set => _name = value; }
    public int Price { get => _price; set => _price = value; }
    public TypeOfCard TypeOfCard { get => _typeOfCard; set => _typeOfCard = value; }
    public int Damage { get => _damage; set => _damage = value; }
    public int Defense { get => _defense; set => _defense = value; }
    public int ManaCost { get => _manaCost; set => _manaCost = value; }

    public override string ToString()
    {
        return "Name: " + _name + "\nPrice: " + _price + "\nType of Card: " + _typeOfCard.ToString() + "\nDamage: " + _damage + "\nDefense: " + _defense + "\nMana Costs: " + _manaCost + "\n";
    }
}
