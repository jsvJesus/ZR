package ui.controls
{
   import com.dvalimona.components.Label;
   import com.dvalimona.components.PushButton;
   import flash.display.Bitmap;
   import flash.display.DisplayObjectContainer;
   import flash.display.Sprite;
   import flash.events.MouseEvent;
   
   public class SimpleBitmapButton extends PushButton
   {
      private var _bitmap:Bitmap;
      
      public function SimpleBitmapButton(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0, label:String = "", defaultHandler:Function = null, bitmap:Bitmap = null, _$:String = "")
      {
         this.bitmap = bitmap;
         super(parent,xpos,ypos,label,defaultHandler,_$);
      }
      
      public function get bitmap() : Bitmap
      {
         return this._bitmap;
      }
      
      public function set bitmap(value:Bitmap) : void
      {
         this._bitmap = value;
      }
      
      override protected function addChildren() : void
      {
         _face = new Sprite();
         _face.addChild(this.bitmap);
         _face.mouseEnabled = false;
         addChild(_face);
         _label = new Label(null,0,0,"",40);
         trace(this,"addChildren",_labelText);
         if(Boolean(_labelText))
         {
            _label.text = _labelText;
         }
         addChild(_label);
         addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseGoDown);
         addEventListener(MouseEvent.ROLL_OVER,this.onMouseOver);
      }
      
      override public function draw() : void
      {
         trace(this,"draw",_labelText);
         if(Boolean(_label))
         {
            _label.text = _labelText;
            _label.autoSize = true;
            _label.draw();
            if(_label.width > _width - 4)
            {
               _label.autoSize = false;
               _label.width = _width - 4;
            }
            else
            {
               _label.autoSize = true;
            }
            _label.draw();
            _label.move(_width / 2 - _label.width / 2,_height + _label.height / 2);
         }
      }
      
      override protected function onMouseGoDown(event:MouseEvent) : void
      {
         _down = true;
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      override protected function onMouseGoUp(event:MouseEvent) : void
      {
         if(_toggle && _over)
         {
            _selected = !_selected;
         }
         _down = _selected;
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMouseGoUp);
      }
      
      override protected function onMouseOver(event:MouseEvent) : void
      {
         _over = true;
         addEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
      }
      
      override protected function onMouseOut(event:MouseEvent) : void
      {
         _over = false;
         if(_down)
         {
         }
         removeEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
      }
   }
}

