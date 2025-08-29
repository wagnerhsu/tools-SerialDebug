using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common;

public class ReadOnlyComboBox : ComboBox
{
    private bool _readOnly = false;

    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            _readOnly = value;
            if (_readOnly)
            {
                this.DropDownStyle = ComboBoxStyle.DropDownList;
                this.MouseClick -= ReadOnlyComboBox_MouseClick;
                this.MouseClick += ReadOnlyComboBox_MouseClick;
            }
            else
            {
                this.MouseClick -= ReadOnlyComboBox_MouseClick;
            }
        }
    }

    private void ReadOnlyComboBox_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_readOnly)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (_readOnly)
            e.Handled = e.SuppressKeyPress = true;
        else
            base.OnKeyDown(e);
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (_readOnly)
            e.Handled = true;
        else
            base.OnKeyPress(e);
    }
}
