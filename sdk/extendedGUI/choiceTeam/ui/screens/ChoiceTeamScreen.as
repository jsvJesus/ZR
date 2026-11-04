package ui.screens
{
   import com.dvalimona.components.Label;
   import com.dvalimona.components.PushButton;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.display.BitmapData;
   import flash.display.Sprite;
   import flash.events.Event;
   import flash.events.MouseEvent;
   import ui.UIScreen;
   import ui.controls.ChoiceTeamButton;
   
   public class ChoiceTeamScreen extends UIScreen
   {
      public static const ON_RED:String = "onRed";
      
      public static const ON_BLUE:String = "onBlue";
      
      public static const VERSION:String = "1.0.0";
      
      private var CHOICE_TEAM:String = "CHOICE_TEAM_SCREEN";
      
      protected var buttons:Sprite;
      
      protected var red:Sprite;
      
      protected var blue:Sprite;
      
      protected var redButton:ChoiceTeamButton;
      
      protected var blueButton:PushButton;
      
      protected var caption:Label;
      
      protected var buttonSpeed:Number = 1;
      
      protected var gap:Number = 0.01;
      
      protected var originalWidth:Number;
      
      protected var originalHeight:Number;
      
      protected var yShift:Number = -0.05;
      
      public function ChoiceTeamScreen()
      {
         super(this.CHOICE_TEAM);
      }
      
      override protected function preinit() : void
      {
         this.horizontalFit = 100;
         this.verticalFit = 100;
      }
      
      override protected function init() : void
      {
         this.buttons = new Sprite();
         this.addChild(this.buttons);
         this.caption = new Label(this.buttons,0,0,"",60,"ChoiceTeam.header");
         var redBitmapData:BitmapData = new red_png();
         this.red = new Sprite();
         this.buttons.addChild(this.red);
         this.redButton = new ChoiceTeamButton(this.red,0,-150,"",this.redHandler,redBitmapData);
         this.redButton.$ = "ChoiceTeam.redTeam";
         this.redButton.setSize(redBitmapData.width,redBitmapData.height);
         this.redButton.addEventListener(MouseEvent.MOUSE_OVER,this.onRedMouseOver);
         this.redButton.addEventListener(MouseEvent.ROLL_OUT,this.onRedMouseOut);
         var blueBitmapData:BitmapData = new blue_png();
         this.blue = new Sprite();
         this.buttons.addChild(this.blue);
         this.blueButton = new ChoiceTeamButton(this.blue,0,-150,"",this.blueHandler,blueBitmapData);
         this.blueButton.$ = "ChoiceTeam.blueTeam";
         this.blueButton.setSize(blueBitmapData.width,blueBitmapData.height);
         this.blueButton.addEventListener(MouseEvent.MOUSE_OVER,this.onBlueMouseOver);
         this.blueButton.addEventListener(MouseEvent.ROLL_OUT,this.onBlueMouseOut);
         this.originalWidth = this.buttons.width;
         this.originalHeight = this.buttons.height;
      }
      
      protected function onBlueMouseOver(event:MouseEvent) : void
      {
         this.buttons.setChildIndex(this.blue,this.buttons.numChildren - 1);
         TweenMax.to(this.blue,this.buttonSpeed,{
            "scaleX":1.1,
            "scaleY":1.1,
            "alpha":1,
            "ease":Expo.easeOut
         });
         TweenMax.to(this.red,this.buttonSpeed,{
            "scaleX":0.9,
            "scaleY":0.9,
            "alpha":0.7,
            "ease":Expo.easeOut
         });
      }
      
      protected function onBlueMouseOut(event:MouseEvent) : void
      {
         TweenMax.to(this.blue,this.buttonSpeed,{
            "scaleX":1,
            "scaleY":1,
            "alpha":1,
            "ease":Expo.easeOut
         });
         TweenMax.to(this.red,this.buttonSpeed,{
            "scaleX":1,
            "scaleY":1,
            "alpha":1,
            "ease":Expo.easeOut
         });
      }
      
      protected function onRedMouseOver(event:MouseEvent) : void
      {
         this.buttons.setChildIndex(this.red,this.buttons.numChildren - 1);
         TweenMax.to(this.red,this.buttonSpeed,{
            "scaleX":1.1,
            "scaleY":1.1,
            "alpha":1,
            "ease":Expo.easeOut
         });
         TweenMax.to(this.blue,this.buttonSpeed,{
            "scaleX":0.9,
            "scaleY":0.9,
            "alpha":0.7,
            "ease":Expo.easeOut
         });
      }
      
      protected function onRedMouseOut(event:MouseEvent) : void
      {
         TweenMax.to(this.red,this.buttonSpeed,{
            "scaleX":1,
            "scaleY":1,
            "alpha":1,
            "ease":Expo.easeOut
         });
         TweenMax.to(this.blue,this.buttonSpeed,{
            "scaleX":1,
            "scaleY":1,
            "alpha":1,
            "ease":Expo.easeOut
         });
      }
      
      private function redHandler(event:Event = null) : void
      {
         trace("red");
         this.dispatchEvent(new Event(ON_RED));
      }
      
      private function blueHandler(event:Event = null) : void
      {
         trace("blue");
         this.dispatchEvent(new Event(ON_BLUE));
      }
      
      override protected function draw() : void
      {
         if(Boolean(this.buttons))
         {
            this.buttons.x = StalkerBase.stage.stageWidth / 2;
            this.buttons.y = StalkerBase.stage.stageHeight / 2 + StalkerBase.stage.stageHeight * this.yShift;
            trace(this.originalWidth,this.originalHeight);
            this.buttons.scaleX = this.buttons.scaleY = StalkerBase.stage.stageWidth * 0.5 / this.originalWidth;
            this.caption.x = -this.caption.width / 2;
            this.caption.y = -270;
         }
         if(Boolean(this.red))
         {
            this.red.x = -this.gap * StalkerBase.stage.stageWidth;
            this.red.y = 0;
         }
         if(Boolean(this.blue))
         {
            this.blue.x = this.gap * StalkerBase.stage.stageWidth;
            this.blue.y = 0;
         }
         if(Boolean(this.redButton))
         {
            this.redButton.x = -this.redButton.width + 100;
         }
         if(Boolean(this.blueButton))
         {
            this.blueButton.x = -100;
         }
         trace(this,"draw",this.red,StalkerBase.stage.stageWidth);
      }
      
      override protected function onMouseMove(event:MouseEvent) : void
      {
         this.doMouseFollow();
      }
      
      private function doMouseFollow() : void
      {
         var xDiff:Number = this.stage.mouseX / (this.stage.stageWidth / 2) - 1;
         var yDiff:Number = this.stage.mouseY / (this.stage.stageHeight / 2) - 1;
         if(Boolean(this.buttons))
         {
            TweenMax.killTweensOf(this.buttons);
            TweenMax.to(this.buttons,3,{
               "rotationX":yDiff * 1,
               "rotationY":xDiff * 1 * -1,
               "x":StalkerBase.stage.stageWidth / 2 - xDiff * StalkerBase.stage.stageWidth * 0.02,
               "y":StalkerBase.stage.stageHeight / 2 - yDiff * StalkerBase.stage.stageHeight * 0.02 + StalkerBase.stage.stageHeight * this.yShift,
               "ease":Expo.easeOut
            });
         }
      }
      
      private function drawDebugRect() : void
      {
         this.minWidth = 200;
         this.minHeight = 200;
         trace("drawDebugRect",this.width,minWidth);
         this.graphics.clear();
         this.graphics.beginFill(Math.random() * 16777215,0.2);
         this.graphics.drawRoundRect(fit.x,fit.y,fit.width,fit.height,20,20);
         this.graphics.endFill();
         trace(this.width);
      }
   }
}

