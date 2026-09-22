# Design: Learner Experience Quality

## Decisions

- Define one learner navigation model with current-location, course context, progress, and primary next action visible at each depth.
- Use shared Razor partials/tag helpers for loading, empty, error, success, confirmation, and disabled states.
- Preserve server-rendered progressive enhancement; forms remain usable without client-side scripts.
- Require semantic landmarks, one page heading, visible keyboard focus, labelled controls, logical tab order, and WCAG 2.2 AA contrast targets.
- Use responsive breakpoints based on content fit, not device names; tables provide a usable narrow-screen alternative.

## Failure handling

Transient failures retain user input where safe, identify what failed, and expose retry. Permission failures use the existing authorization boundary and do not reveal private resources.

## Verification

Add Razor view tests for state variants, automated accessibility checks for representative pages, keyboard smoke tests, and viewport screenshots for learner journeys.
