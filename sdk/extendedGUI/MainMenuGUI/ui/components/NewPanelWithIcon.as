package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import communication.*;
   import flash.display.*;
   import flash.events.*;
   import flash.utils.*;
   
   public class NewPanelWithIcon extends Component
   {
      protected var back:ui.components.BlackPanel;
      
      protected var caption:MenuButton2;
      
      protected var description:TextShadowed;
      
      protected var standartIcon:Sprite;
      
      protected var standartBox:VBox;
      
      protected var standartView:Sprite;
      
      protected var _minMode:Boolean = true;
      
      protected var _icon:Bitmap;
      
      protected var is_over:Boolean = false;
      
      public function NewPanelWithIcon(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         this.width = this.preferredWidth;
         super(parent,xpos,ypos);
         this.initListeners();
         this.addEventListener(MouseEvent.ROLL_OVER,this.onMouseOver);
         this.addEventListener(MouseEvent.ROLL_OUT,this.onMouseOut);
         this.addEventListener(MouseEvent.CLICK,this.onMouseClick);
         stage.addEventListener(Event.RESIZE,this.onResize);
         this.mouseChildren = false;
      }
      
      protected function get preferredWidth() : uint
      {
         return 468;
      }
      
      override protected function addChildren() : void
      {
         this.back = new BlackPanel();
         this.back.alpha = 0.9;
         this.addChild(this.back);
         this.caption = new MenuButton2(this);
         this.caption.$ = "extendedGUI.RootWindow.premiumButton";
         this.caption.font = Base.fontName;
         this.caption.clipContent = true;
         this.caption.labelOverColor = Style.GOLD_OVER;
         this.caption.underline = false;
         this.caption.size = 22;
         this.caption.align = Label.RIGHT;
         this.caption.mouseEnabled = false;
         this.standartView = new Sprite();
         this.standartIcon = new Sprite();
         this.standartView.addChild(this.standartIcon);
         this.standartView.mouseChildren = false;
         this.standartBox = new VBox(this.standartView);
         this.standartBox.spacing = 1;
         this.standartBox.alignment = VBox.LEFT;
         this.standartBox.debug = false;
         this.standartBox.mouseChildren = false;
         this.standartBox.mouseEnabled = false;
         this.description = new TextShadowed(this.standartBox);
         this.description.editable = false;
         this.description.selectable = false;
         this.description.autoHeight = true;
         this.description.size = 20;
         this.description.color = 11776934;
         this.description.paddingLeft = 36;
         this.description.paddingRight = 10;
         this.description.paddingTop = 18;
         this.description.paddingBottom = 10;
         this.description.$ = "extendedGUI.RootWindow.premiumHint1";
         this.description.debug = false;
         this.description.draw();
         this.description.mouseEnabled = false;
         setTimeout(this.updateView,500);
         this.mouseChildren = false;
      }
      
      public function get minMode() : *
      {
         if(Base.stage.stageWidth >= 1100)
         {
            return false;
         }
         return this._minMode;
      }
      
      public function set minMode(value:Boolean) : *
      {
         if(this.minMode == value)
         {
            return;
         }
         this._minMode = value;
         this.updateView();
      }
      
      public function updateView() : void
      {
         if(!this.contains(this.standartView))
         {
            this.addChild(this.standartView);
         }
         for(var i:* = this.standartIcon.numChildren - 1; i >= 0; i--)
         {
            this.standartIcon.removeChildAt(i);
         }
         this.standartBox.x = 160 - 62;
         this.standartBox.y = 40;
         if(this._icon != null)
         {
            this._icon.width = 128;
            this._icon.height = 128;
         }
         this.caption.visible = true;
         this.back.y = 0;
         if(this._icon != null)
         {
            this.standartIcon.addChild(this._icon);
            this._icon.x = (138 - this._icon.width) / 2;
         }
         if(this.minMode)
         {
            this.caption.visible = false;
            if(this._icon != null)
            {
               this.standartIcon.addChild(this._icon);
               this._icon.x = (128 - this._icon.width) / 2;
            }
         }
         this.description.visible = !this.minMode;
         this.description.width = this.preferredWidth - this.standartBox.x - 50 - 0;
         setTimeout(this.tuneHeight,100,true);
         setTimeout(this.tuneHeight,400,true);
         invalidate();
      }
      
      public function onResize() : void
      {
         this.updateView();
         setTimeout(this.updateView,500);
      }
      
      public function set icon(value:Bitmap) : *
      {
         this._icon = value;
         this.updateView();
      }
      
      public function title(value:String, localised:Boolean = true) : *
      {
         if(localised)
         {
            this.caption.label = value;
         }
         else
         {
            this.caption.$ = value;
         }
         this.tuneHeight(true);
      }
      
      public function text(value:String, localised:Boolean = true) : *
      {
         if(localised)
         {
            this.description.text = value;
         }
         else
         {
            this.description.$ = value;
         }
         this.tuneHeight(true);
      }
      
      protected function onCaptionClick(event:MouseEvent) : void
      {
         dispatchEvent(new Event("onCaptionClick"));
      }
      
      protected function tuneHeight(flag:Boolean) : void
      {
         var th:Number = Math.max(this.standartIcon.y + 105 + 10,this.description.y + this.description.height + 10);
         var tw:Number = Number(this.preferredWidth);
         this.description.paddingTop = 18;
         this.description.size = 20;
         if(this.minMode)
         {
            th = 128;
            this.description.size = 16;
            this.description.paddingTop = 5;
            tw = 128;
         }
         TweenMax.to(this,0.8,{
            "height":th,
            "width":tw,
            "ease":Expo.easeOut,
            "onComplete":this.initListeners
         });
      }
      
      protected function initListeners() : *
      {
      }
      
      override public function draw() : void
      {
         super.draw();
         this.back.width = width;
         this.back.height = height;
         this.caption.x = 20;
         this.caption.y = 20;
         if(this.minMode)
         {
            this.standartIcon.y = 20;
         }
         else
         {
            this.standartIcon.y = 50;
         }
         this.description.x = 34;
      }
      
      protected function onMouseOver(e:MouseEvent) : *
      {
         if(this.is_over)
         {
            return;
         }
         this.is_over = true;
         this.back.backColor = 3947580;
         if(!this.caption.over)
         {
            PlaySounds.onOver();
         }
         this.caption.over = true;
         if(this.minMode)
         {
            this.minMode = false;
         }
      }
      
      protected function onMouseOut(e:MouseEvent) : *
      {
         if(Boolean(e.relatedObject) && (this.contains(e.relatedObject) || Boolean(this.standartBox.contains(e.relatedObject)) || Boolean(this.standartView.contains(e.relatedObject))))
         {
            return;
         }
         if(!this.is_over)
         {
            return;
         }
         this.back.backColor = 0;
         this.caption.over = false;
         this.is_over = false;
         if(Base.stage.stageWidth < 1100)
         {
            this.minMode = true;
         }
      }
      
      protected function onMouseClick(e:MouseEvent) : *
      {
         this.onCaptionClick(e);
         PlaySounds.onDown();
      }
   }
}

