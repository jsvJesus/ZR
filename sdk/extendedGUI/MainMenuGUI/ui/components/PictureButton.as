package ui.components
{
   import com.dvalimona.components.*;
   import flash.display.*;
   import flash.events.*;
   import flash.net.*;
   
   public class PictureButton extends Component
   {
      public var background:Sprite;
      
      private var loader:Loader = new Loader();
      
      private var _texturePath:URLRequest;
      
      private var _texture:Bitmap = new Bitmap();
      
      private var _selected:Boolean = false;
      
      private var _down:Boolean = false;
      
      private var _over:Boolean = false;
      
      private var _textureWidth:Number = 0;
      
      private var _textureHeight:Number = 0;
      
      public var upColorAlpha:Number;
      
      public var downColorAlpha:Number;
      
      public var overColorAlpha:Number;
      
      public var downColor:uint;
      
      public var overColor:uint;
      
      public var upColor:uint;
      
      public var index:int = 0;
      
      public var additionalData:int = 0;
      
      public function PictureButton(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
         this.upColorAlpha = 0.5;
         this.downColorAlpha = 1;
         this.overColorAlpha = 0.8;
         this.downColor = 0;
         this.overColor = 0;
         this.upColor = 0;
         this.addEventListener(MouseEvent.MOUSE_OVER,this.onMouseOver);
         this.addEventListener(MouseEvent.MOUSE_OUT,this.onMouseOut);
         this.addEventListener(MouseEvent.MOUSE_DOWN,this.onMouseDown);
         this.addEventListener(MouseEvent.MOUSE_UP,this.onMouseUp);
      }
      
      public function set texturePath(value:String) : *
      {
         this._texturePath = new URLRequest(value);
         this.loader.contentLoaderInfo.addEventListener(Event.COMPLETE,this.onTextureLoad);
         this.loader.load(this._texturePath);
      }
      
      public function get texturePath() : String
      {
         return this._texturePath;
      }
      
      public function set selected(value:Boolean) : *
      {
         this._selected = value;
         this.draw();
      }
      
      public function get selected() : Boolean
      {
         return this._selected;
      }
      
      public function set over(value:Boolean) : *
      {
         this._over = value;
         this.draw();
      }
      
      public function get over() : Boolean
      {
         return this._over;
      }
      
      public function set down(value:Boolean) : *
      {
         this._down = value;
         this.draw();
      }
      
      public function get down() : Boolean
      {
         return this._down;
      }
      
      override public function set width(value:Number) : void
      {
         super.width = value;
         this.updateTextureSize();
      }
      
      private function onTextureLoad(e:Event) : void
      {
         this.loader.contentLoaderInfo.removeEventListener(Event.COMPLETE,this.onTextureLoad);
         this._texture = Bitmap(this.loader.content);
         this._textureHeight = this._texture.height;
         this._textureWidth = this._texture.width;
         this.updateTextureSize();
         this.addChild(this._texture);
         this.draw();
         parent.draw();
      }
      
      private function updateTextureSize() : *
      {
         var ratio:Number = this._textureWidth / this._textureHeight;
         if(this._textureWidth > this.width)
         {
            this._texture.width = this.width;
         }
         else
         {
            this._texture.width = this._textureWidth;
         }
         this._texture.height = this._texture.width / ratio;
         this.height = this._texture.height;
         this._texture.x = (this.width - this._texture.width) / 2;
         this._texture.y = (this.height - this._texture.height) / 2;
         (parent as HBox).fixedHeight = this.height + 20;
      }
      
      public function onMouseOver(event:MouseEvent) : *
      {
         this.over = true;
      }
      
      public function onMouseOut(event:MouseEvent) : *
      {
         this.over = false;
      }
      
      public function onMouseDown(event:MouseEvent) : *
      {
         this.down = true;
      }
      
      public function onMouseUp(event:MouseEvent) : *
      {
         this.down = false;
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         this.background = new Sprite();
         this.background.mouseEnabled = false;
         this.addChild(this.background);
      }
      
      override public function draw() : void
      {
         super.draw();
         this.background.graphics.clear();
         if(this.selected)
         {
            this._texture.alpha = this.downColorAlpha;
            this.background.graphics.beginFill(this.downColor,this.downColorAlpha);
            return;
         }
         if(this.over)
         {
            if(this.down)
            {
               this._texture.alpha = this.downColorAlpha;
               this.background.graphics.beginFill(this.downColor,this.downColorAlpha);
            }
            else
            {
               this._texture.alpha = this.overColorAlpha;
               this.background.graphics.beginFill(this.overColor,this.overColorAlpha);
            }
         }
         else
         {
            this._texture.alpha = this.upColorAlpha;
            this.background.graphics.beginFill(this.upColor,this.upColorAlpha);
         }
      }
   }
}

