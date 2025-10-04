# TODO

## Why?

I found many TODOs in the code, so I decided to write them down to get an overview of what still needs to be done.



## TODOs

* redshirts in city
  * https://forums.civfanatics.com/threads/red-unhappy-people.251311/

### SaveDataAdapter.Set.cs

```cs
cities[i].ActualSize = data.ActualSize;
cities[i].VisibleSize = data.ActualSize; // TODO: Implement Visible Size
cities[i].CurrentProduction = data.CurrentProduction;
cities[i].BaseTrade = 0; // TODO: Implement trade
...
SetArray(ref cities[i], nameof(SaveData.City.TradingCities), 0xFF, 0xFF, 0xFF);
```

1. ActualSize vs. VisibleSize.
   - Looks like, as if enemy city size for the player is only shown as VisibleSize, but not as ActualSize.
   - This may be a fog of war kind of thing.
2. Trading is not implemented yet.
