using UnityEngine;

public class Card : MonoBehaviour
{
    public string _imageFileName;
    public string _name;
    public int _price;
    public TypeOfCard _typeOfCard;
    public int _damage;
    public int _defense;
    public int _manaCost;
    public int _duration;
    public int _effectValue;

    public Card(string imageFileName, string name, int price, TypeOfCard typeOfCard, int damage, int defense, int manaCost, int duration, int effectValue)
    {
        _imageFileName = imageFileName;
        _name = name;
        _price = price;
        _typeOfCard = typeOfCard;
        _damage = damage;
        _defense = defense;
        _manaCost = manaCost;
        _duration = duration;
        _effectValue = effectValue;
    }

    public string ImageFileName { get => _imageFileName; set => _imageFileName = value; }
    public string Name { get => _name; set => _name = value; }
    public int Price { get => _price; set => _price = value; }
    public TypeOfCard TypeOfCard { get => _typeOfCard; set => _typeOfCard = value; }
    public int Damage { get => _damage; set => _damage = value; }
    public int Defense { get => _defense; set => _defense = value; }
    public int ManaCost { get => _manaCost; set => _manaCost = value; }
    public int Duration { get => _duration; set => _duration = value; }
    public int EffectValue { get => _effectValue; set => _effectValue = value; }


    public override string ToString()
    {
        return "Name: " + _name + "\nPrice: " + _price + "\nType of Card: " + _typeOfCard.ToString() + "\nDamage: " + _damage + "\nDefense: " + _defense + "\nMana Costs: " + _manaCost + "\nDuration: " + _duration + "\nEffect Value: " + _effectValue + "\n";
    }

    public Card Clone()
    {
        return (Card)this.MemberwiseClone();
    }
}
