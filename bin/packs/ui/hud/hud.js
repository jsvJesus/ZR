"use strict";

const elements = {
    hud: document.getElementById("hud"),
    condition: document.getElementById("condition"),

    healthBar: document.getElementById("health-bar"),
    healthValue: document.getElementById("health-value"),

    staminaBar: document.getElementById("stamina-bar"),
    staminaValue: document.getElementById("stamina-value"),

    satietyBar: document.getElementById("satiety-bar"),
    satietyValue: document.getElementById("satiety-value"),

    hydrationBar: document.getElementById("hydration-bar"),
    hydrationValue: document.getElementById("hydration-value"),

    weightValue: document.getElementById("weight-value"),

    hunger: document.getElementById("effect-hunger"),
    thirst: document.getElementById("effect-thirst"),
    overload: document.getElementById("effect-overload"),

    bleeding: document.getElementById("effect-bleeding"),
    radiation: document.getElementById("effect-radiation"),
    intoxication: document.getElementById("effect-intoxication")
};

function clamp(value, minimum, maximum) {
    return Math.max(
        minimum,
        Math.min(maximum, value)
    );
}

function setBar(element, value, maximum) {
    const ratio = maximum > 0
        ? clamp(value / maximum, 0, 1)
        : 0;

    element.style.width = `${ratio * 100}%`;
}

function setEffect(element, visible, text) {
    element.hidden = !visible;

    if (text) {
        element.textContent = text;
    }
}

window.updateHud = function (state) {
    if (!state) {
        return;
    }

    setBar(
        elements.healthBar,
        state.hitPoints,
        state.maxHitPoints
    );

    elements.healthValue.textContent =
        `${Math.ceil(state.hitPoints)} / ${Math.round(state.maxHitPoints)}`;

    setBar(
        elements.staminaBar,
        state.stamina,
        state.maxStamina
    );

    elements.staminaValue.textContent =
        `${Math.floor(state.stamina)} / ${Math.round(state.maxStamina)}`;

    setBar(
        elements.satietyBar,
        state.satiety,
        1
    );

    elements.satietyValue.textContent =
        `${Math.round(state.satiety * 100)}%`;

    setBar(
        elements.hydrationBar,
        state.hydration,
        1
    );

    elements.hydrationValue.textContent =
        `${Math.round(state.hydration * 100)}%`;

    elements.weightValue.textContent =
        `${state.carryWeight.toFixed(1)} / ${state.maxWeight.toFixed(1)} кг`;

    setEffect(
        elements.hunger,
        state.isHungry
    );

    setEffect(
        elements.thirst,
        state.isThirsty
    );

    setEffect(
        elements.overload,
        state.isOverloaded
    );

    setEffect(
        elements.bleeding,
        state.bleedingStacks > 0,
        `КРОВОТЕЧЕНИЕ ×${state.bleedingStacks}`
    );

    setEffect(
        elements.radiation,
        state.radiationStacks > 0,
        `РАДИАЦИЯ ×${state.radiationStacks}`
    );

    setEffect(
        elements.intoxication,
        state.intoxicationStacks > 0,
        `ИНТОКСИКАЦИЯ ×${state.intoxicationStacks}`
    );

    elements.hud.classList.toggle(
        "critical",
        state.isCriticalHealth
    );

    elements.hud.classList.toggle(
        "overloaded",
        state.isOverloaded
    );

    if (!state.isAlive) {
        elements.condition.textContent = "МЁРТВ";
    } else if (state.isCriticalHealth) {
        elements.condition.textContent = "КРИТИЧЕСКОЕ";
    } else {
        elements.condition.textContent = "НОРМА";
    }

    elements.hud.classList.add("ready");
};