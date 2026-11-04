package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.*;
   
   public class HeaderPanel extends Component
   {
      private var _useBackground:Boolean = true;
      
      private var _label:LabelShadowed;
      
      protected var panelWidth:uint = 300;
      
      protected var panelBorder:uint = 10;
      
      protected var contentShift:uint = 15;
      
      protected var lineHeight:uint = 25;
      
      protected var _back:Sprite;
      
      protected var headerBoxLeft:HBox;
      
      protected var headerBoxRight:HBox;
      
      public function HeaderPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
      }
      
      public function get useBackground() : Boolean
      {
         return this._useBackground;
      }
      
      public function set useBackground(value:Boolean) : void
      {
         this._useBackground = value;
         invalidate();
      }
      
      public function get label() : LabelShadowed
      {
         return this._label;
      }
      
      public function set label(value:LabelShadowed) : void
      {
         this._label = value;
      }
      
      public function get slot() : HBox
      {
         return this.headerBoxRight;
      }
      
      override protected function addChildren() : void
      {
         this._back = new Sprite();
         this._back.mouseEnabled = false;
         addChild(this._back);
         this.headerBoxLeft = new HBox(this,0,this.panelBorder);
         this.headerBoxLeft.horizontalAlign = HBox.LEFT;
         this.headerBoxLeft.alignment = HBox.BOTTOM;
         this.headerBoxLeft.fixedHeight = 30;
         this.headerBoxRight = new HBox(this,0,this.panelBorder);
         this.headerBoxRight.horizontalAlign = HBox.RIGHT;
         this.headerBoxRight.alignment = HBox.BOTTOM;
         this.headerBoxRight.fixedHeight = 26;
         this.headerBoxRight.spacing = 1;
         this.headerBoxRight.debug = false;
         this._label = new LabelShadowed(this.headerBoxLeft,0,0);
         this._label.text = "";
         this._label.color = 10526608;
         this._label.shadowColor = 0;
         this._label.shadowAlpha = 0.9;
         this._label.shadowSize = 1;
         this._label.size = 20;
         this._label.clipContent = false;
         this._label.debug = false;
         this._label.autoSize = true;
         this._label.paddingLeft = 10;
      }
      
      protected function updateContent() : void
      {
      }
      
      override public function draw() : void
      {
         super.draw();
         this._back.graphics.clear();
         if(this._useBackground)
         {
            this._back.graphics.beginBitmapFill(Style.backgroundBitmap);
            this._back.graphics.drawRect(0,0,_width,_height);
            this._back.graphics.endFill();
            this._back.alpha = 0.5;
         }
         this.headerBoxRight.fixedWidth = _width;
      }
   }
}

