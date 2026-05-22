using CivOne.Tiles;

namespace CivOne.Units;

/// <summary>
/// Concrete implementation of IConfrontContext.
/// Adapts BaseUnit state and tile data to the delegate's context interface.
/// </summary>
internal class ConfrontContextAdapter : IConfrontContext
{
    private readonly BaseUnit _attackingUnit;
    private ITile _moveTarget;
    private int _relX;
    private int _relY;

    public ConfrontContextAdapter(BaseUnit attackingUnit)
    {
        _attackingUnit = attackingUnit;
    }

    public void SetConfrontData(ITile moveTarget, int relX, int relY)
    {
        _moveTarget = moveTarget;
        _relX = relX;
        _relY = relY;
    }

    public IUnit AttackingUnit => _attackingUnit;

    public ITile MoveTarget => _moveTarget;

    public IUnit TargetUnit => _moveTarget.Units.Length > 0 ? _moveTarget.Units[0] : null;

    public byte MovesLeft => _attackingUnit.MovesLeft;

    public byte PartMoves => _attackingUnit.PartMoves;

    public bool Fortify => _attackingUnit.Fortify;

    public bool FortifyActive => _attackingUnit.FortifyActive;

    public bool Veteran
    {
        get => _attackingUnit.Veteran;
        set => _attackingUnit.Veteran = value;
    }

    public bool IsAttackerDiplomat => _attackingUnit is Diplomat;

    public bool IsAttackerCaravan => _attackingUnit is Caravan;

    public bool IsAttackerNuclear => _attackingUnit is Nuclear;

    public bool IsAttackerCannonType => _attackingUnit is Cannon or Musketeers or Riflemen or Armor or Artillery or MechInf;

    public int RelX => _relX;

    public int RelY => _relY;

    public System.Action<int> ConsumeMoves => (moves) =>
    {
        if (_attackingUnit.MovesLeft == 0)
        {
            _attackingUnit.PartMoves = 0;
        }
        else if (_attackingUnit.MovesLeft > 0)
        {
            if (_attackingUnit is Bomber)
            {
                _attackingUnit.SkipTurn();
            }
            else
            {
                _attackingUnit.MovesLeft--;
            }
        }
    };

    public System.Action OnGotoCleared => () =>
    {
        _attackingUnit.Goto = System.Drawing.Point.Empty;
    };

}
